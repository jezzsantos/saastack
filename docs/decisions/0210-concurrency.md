# Concurrency Mitigations

* status: accepted
* date: 2025-0506
* deciders: jezzsantos

# Context and Problem Statement

Concurrency is one of those things that is unavoidable when it comes to programming distributed web-based systems. That's because even with stateless web-based systems, HTTP requests can be received concurrently. If a stateless request changes the state of some entity, it is possible that two or more requests can change the state of the same entity, and if that happens it is possible to have a "race condition" that competes to who changes the state.

In some systems, particularly in event-sourced systems, where order of historical events matters, it is possible that two requests (or two threads) try to update the state of the same aggregate, which can mean that the updates happen at the same time, and thus conflict with each other, since all events must happen in a specific order, and all events must be contiguous.

Consider the following common scenario.

1. Request #1 is received into the API, and changes the state of a specific aggregate.
   * On average any request takes ~500ms to process for this aggregate.
2. Request #2 is received into the API, some ~100ms seconds later, and also changes the state of a specific aggregate, perhaps in the same way, perhaps in a different way, it actually does not matter in general.
3. Both requests will load the same aggregate from the event store into memory and rebuild its in-memory state from those 34 events, and the last event in the event stream is noted as being version v.34.
   * In effect, the current in-mem state of the aggregate is composed of 34 historical events, all ordered by version number, starting at version v.1 through v.34.
4. Now, request #1 begins to create 3 new events, and request #2 begins to create 4 new events. These sets of events could be the same events or different, it makes no difference in fact.
   * Note, at this moment in time it is non-deterministic which request is faster to process - request #1 or request #2, and thus which one gets to save it state first.
5. At some point in time, one of the requests (lets say it is request #2) will try to persist its newly created events, and in order to persist them to the event store, they must be versioned (contiguously) starting with the version AFTER the last event that was loaded in the beginning.
   * In this example, that version is v.35 through to v.38, for the 4 events that request #2 produces.
6. Now, those events will be persisted correctly to the event store, since they match the versions of the actual existing events already stored in the events store. Request #2 succeeds and exits successfully.
7. Next, ~100ms later, request #1 tries to save its 3 new events to the event store, and in its case, these new events will need to be versioned from v.35 through to v.37.
   - Again, from the perspective of this request the last loaded event version was v.34.

8. The event store will detect that the latest saved events in the store are now at v.38, not at v.35, and the event store will throw an exception and complain (quite rightly) that the new events will overwrite existing events, and the request #2 will fail with a concurrency problem.
   - Events can never be overwritten, and thus we have a concurrency problem.


Unintuitively, the concurrency problem above is not so easily resolved by request #1 simply by re-versioning its events, from v38. That is because the state of the aggregate, as it was at v.34 in-memory, is now likely to be very different at v38. And request #1 does not have the events v.35 to v.38 in its current in-memory state.

What has to happen to maintain the integrity of the aggregate for request #1, is that the aggregate is reloaded from the event store, and the aggregate re-processes the request #1 over again to build up the proper state of the aggregate correctly. In other words, the whole of request #1 needs to re-processed to reload the aggregate in the correct state up to v.38, in order to save the next set of events from v.39 through to v41. 

>  Otherwise, it is possible that the aggregate ends up in a corrupted state. 

### Reliability

It is important that this concurrency issue is handled correctly and timely, since there are many ways in which this concurrency mechanism can be triggered. 

A common way to reproduce this problem quite frequently is to have clients (of the API) send many requests in parallel. There are many known ways this can happen in a system of this architecture. most happen accidentally, for example:

1. Badly behaved web clients POST at least two requests to affect the same aggregate at the same time. For example, incorrectly use asynchronous programming that inadvertently post HTTP requests for the same resource. i.e. poorly designed JavaScript promises.
2.  When scaling-out message consumers. In a distributed system, the dreaded "[competing consumers](https://learn.microsoft.com/en-us/azure/architecture/patterns/competing-consumers)" can easily, and inadvertently, be created in all sorts of scenarios. These things will likely create parallel requests to the same resources at the same times. i.e. scaling-out azure functions on FIFO queues, or sometimes scaling out API instances. The combinations of scaling-out different components is sometimes unavoidable. 

 Either way, concurrency is a real problem in any system where state is persistence centrally.

We need to take pre-cautions and provide fault-tolerant mechanisms to deal with it whenever it happens. It cannot be avoided forever.

> Interestingly, concurrency is not just a problem for event-sourced systems, it is also a problem for non-event-sourced systems. For example, in relational databases, concurrency is usually handled by optimistic locking, and can be dealt  with transactions. Nonetheless, errors can happen and need to be retried in some way.

## Considered Options

The options are:
1. Detect and Retry
2. Detect and Fail

## Decision Outcome

`Detect and Fail`

- This option is better than no detection at all. Where data can be corrupted
- We succeed on the first thread, but fail any others coming after. The state of the system is protected. However, the request fails and must be retried somehow, until it succeeds. For user-triggered changes this approach is generally poorly received.
- It requires the least amount of work, not only in prevention, but also in ongoing maintenance.

### Consequences

This is not a sustainable long term approach.

The main challenge with it is that detection will surface many errors and this will make the observability of the system far harder. That is, to sift these errors from genuine errors that  need to be dealt with.  

## (Optional) More Information

We will be seeking a better general solution to this problem that will have us automatically retry in the cases where concurrency is detected and reported.

This mechanism is likely either to be implemented across all APIs, in the API layer, or explicitly programmed in the Application Layer, per use case.