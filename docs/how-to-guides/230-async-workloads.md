# Offload an Async Workload

## Why?

In some use cases, we need to process a multi-step or long running workflow, and/or it involves 3rd party services (over HTTP) to process the data.

Attempting to do these kinds of processing synchronously, and increasing up the API response times, and loading up servers memory and resources, and then risking handling unreliable calls over HTTP (with retries, and backoffs, and timeouts), trying to ignore the very real [Fallacies of Distributed Computing](https://en.wikipedia.org/wiki/Fallacies_of_distributed_computing), and doing that all within the span of a a few hundred milliseconds, is pretty risky in many cases.

Where there are API calls that can be offloaded, they should be offloaded to asynchronous workloads, so that the API request can return as soon as possible AND as a bonus the asynchronous processing can be made "reliable".

> Note: some workloads will require the user either being notified that they are completed, when that happens in the future, and/or a user will need to check back in later to see if the workload is finished. In these cases, some kind of notification mechanism may need to be considered. 

There are many examples of offloading asynchronous workloads in the codebase today, for example:

* Audits - at certain times, the code will record an auditable event, that must be recorded permanently, usually for an action that a user takes, generally related to security or compliance. These events are offloaded, to a queue, and then stored in permanent storage.
* Usages - as the user performs ordinary actions in the API, we record product usage metrics in a 3rd party system. There can be several of these usages in any single API call. These are offloaded to a queue, and then eventually delivered to a 3rd party system like: UserPilot, MixPanel, etc  
* Email/SMS Delivery - whenever any API call wants to send an email message or SMS message to a user, this is offloaded to a queue, and then later a 3rd party email/SMS provider is called to deliver the email or SMS message. The speed at which this message is delivered to a human, is generally pretty important. We tend to see that occurs within a couple of seconds, even with the offloading mechanism in place. 
* Domain/Event notifications - this is a different kind of event, and different mechanism, but domain_events raised from aggregates in any subdomain, are published to topics on message buses, and delivered synchronously to consumers. Read models are also updated in this way, so both read models and consuming aggregates are both eventually consistent.

In all cases, for testability, all offloaded workloads need to be implemented as API calls. Two reasons for this:

1. Testability - we have great tools and patterns for API development. 
2. Interoperability - we can call HTTP APIs from any distributed component or infrastructure (e.g. a Queue Trigger, any Workflow engine, etc.).
3. Load Balancing - We can scale the ApiHosts to any number of instances, and where concurrency is a problem, only one message will be sent to one instance of an ApiHost at any time.

## What is the mechanism?

In general, asynchronous workloads must be processed with reliable mechanisms, such as FIFO queues, or in-order message bus topics/subscriptions.

> Whenever we consider these kinds of mechanisms they must be scalable (scale-out for handling increased load), and they must not introduce unwanted concurrency issues, which, when unwanted, are usually a result of "[competing consumers](https://learn.microsoft.com/en-us/azure/architecture/patterns/competing-consumers)".

### Reliability

Reliability is always critically important in asynchronous workloads. These workloads must handle failures properly, and they often deal with remote systems that may or may not be available at the time they are consumed.

These reliable mechanisms need to both deal with intermittent faults (viz: [Fallacies of Distributed Computing](https://en.wikipedia.org/wiki/Fallacies_of_distributed_computing), and also deal with persistent failures too. For example, the current code has not been designed correctly, or is broken. Patterns like "poison queues/dead-lettering", and "[circuit breaker](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)" are common considerations, so that errors can be reliably diagnosed and resolved properly without losing data.

The outcome of using these reliable mechanisms is that the data causing errors (transient or permanent) is preserved so that the messages that caused the problem can be attempted reliably again later, once a problem has been resolved. 

### Concurrency

Concurrency is another critically important factor for some asynchronous workloads, as most asynchronous workloads/infrastructure involving queues /topics support parallelism to some degree, for maximizing throughput under high load conditions.

Some workloads may only ever deal with requests to produce a single aggregate (i.e. create a new instance for every request). In these cases, concurrency is less of a likelihood. However, some workloads may update the same aggregate, several times, once per request. In these cases, concurrency is a very big problem to resolve, because when the workloads are scaled-out, "competing consumers" and parallel processing can easily, and quite frequently, cause concurrency issues. And these are very hard to diagnose and resolve.

For example, if you have 3 messages on a FIFO queue, and they all update the same aggregate, and we either scale-out the delivery mechanism or we scale-out the consumers, or we process the messages on the queue in batches (in parallel), it is highly likely that multiple API calls to update the same aggregate will occur at the same time (or within milliseconds of each other) and this will almost certainly cause concurrency errors, saving the state of that aggregate for each change in each request.

Bottom line is that the asynchronous mechanism we choose, the workload we are dealing with, and how we scale it out, matters a great deal.

## Where to start?

First decide what the workload is and whether offloading it to an asynchronous process is the right decision. 

If it is, then decide how it should scale-out when load increases? Consider the infrastructure used, and how it is configured, very carefully.

Is concurrency an issue? Will parallelizing the load cause concurrency issues?

> Warning: Even with FIFO queues if you allow parallelizing of multiple messages (say in batches), when you are changing the same aggregate with parallel requests, then concurrency issues are very likely to occur!

If concurrency looks to be an issue for your workload, then you will HAVE to make very sure that when infrastructure is scaled-out, you correctly handle concurrency.

At the moment, in this codebase, we have a few different kinds of workloads, that work quite differently, for different reasons. Understanding these differences is important. You cannot just reuse the mechanisms without understanding these differences.

### Workload1 - Ancillary FIFO Queues

These workloads process asynchronous "ancillary" workloads that exist to offload non-critical, non-blocking, non-user-facing tasks that mainly send data to other 3rd party systems, or to long term storage.

For example, sending emails/SMS message notifications, storing permanent audits, and tracking product usage metrics.     

These workloads, have these characteristics:

* The data they affect is pretty atomic and independent (unrelated) between requests.
  * Thus, these workloads can be parallelized and processed concurrently (for scale), and should never affect the state of each other.
* Order of arrival and delivery, is not critical for any of these workloads.
  * Since, if it was, we could not parallelize them nor scale them out to multiple consumers.
* Fault-tolerance (in terms of reliability and timeliness) does vary between each workload.
  * For example, if an email/SMS is not delivered in a reasonably timely manner, human users will eventually notice, and this will degrade their experience. Consider an MFA SMS message code not arriving within a few minutes.
  * However, if a product metric fails to be delivered to a 3rd party system in a timely manner, it probably will not be noticed immediately, and certainly will not degrade overall system performance. However, it needs to arrive at some point in the future, for historical comparisons.
* These workloads operate reliably in "peek-lock" mode, to ensure guaranteed delivery, and this also remove duplicate requests, when there are multiple competing consumers present.

These workloads represent the lowest bar for any asynchronous workloads.

The common patterns used here are:

* Reliable queues (FIFO is not essential), concurrency is not a factor.
  * For example: Azure Storage Queues, or Azure Service Bus Queues
  * For example: AWS SNS Queues
* Reliable delivery mechanisms, that can be scaled out.
  * For example: Azure Functions
  * For example: AWS Lambdas 
* API workload to process data (for testability and load balancing).
  * For example: APIs 

### Workload2 - Message Bus Topic Subscriptions

These workloads deliver `domain_event` notifications to subscribers of other subdomains in the same system. These workloads are absolutely critical to the overall system working normally, and maintaining the integrity of the system and relationships between components of the system.

These workloads have these characteristics:

* The data they affect might be atomic, but it has co-dependencies on other parts of the system (e.g. involved in long sagas).

* Order in delivery is critical. The consumers of these subscription MUST receive events in precisely the same order they were produced, since the overall state of the system depends exactly on the state before the event was raised.

* Since order is so critical, if a single message fails, it cannot be sidelined into a poison-queue/dead letter queue, otherwise the messages behind it are then handled next, and this results in out of order delivery.
* Events that occur in this workload will be subscribed to by multiple consumers, who all need to receive the same events, in the same order. Hence the use of topics and subscriptions, as opposed to using FIFO queues and multiple-consumers. When scaled-out, these consumers must still not compete.

- Fault-tolerance is also critical, since many of the consumers participate in longer [sagas](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga). Errors in delivery and completion of workloads is non-negotiable, as otherwise the state of the whole system would easily become corrupted.
- These workloads operate reliably in "peek-lock" mode, to ensure guaranteed delivery (with timeouts), and also requires mitigation against duplication.

These workloads represent the very pinnacle of critical asynchronous workloads, which is far harder to achieve properly in practice.

The common patterns used here are:

* Reliable message bus topics (multiple non-competing consumers).
  * For example: Azure Service Bus Topics
  * For example: AWS SNS Topics
* Reliable delivery mechanisms, that can be scaled out.
  * For example: Azure Functions
  * For example: AWS Lambdas 
* API workload to process data (for testability and load balancing).
  * For example: APIs

### Workload 3 - Domain Specific FIFO Queues

Like the two workloads above, this workload is a unique combination of the two.

It is generally used within individual subdomains to process data from aggregates, with 3rd parties, that in turn, asynchronously change the state of the issuing aggregate.

In these cases, concurrency is a major issue to avoid at all costs.

These workloads have these characteristics:

* The data affected might be atomic, but the data is related to the aggregate that, produces both the message, and consumes any data as a result.

* Order in delivery could be critical, for this specific aggregate. 
  * If order is not critical for this aggregate, then the message could fail, and it could be sidelined into a poison-queue/dead letter queue.

* Fault-tolerance is also critical. Errors in delivery and completion of workloads is non-negotiable, as otherwise the state of the whole system would easily become corrupted.

- These workloads operate reliably in "peek-lock" mode, to ensure guaranteed delivery (with timeouts), and also requires mitigation against duplication.

The common patterns used here are:

* Reliable queues (FIFO probably essential), but concurrency is not permitted.
  * For example: Azure Storage Queues, or Azure Service Bus Queues
  * For example: AWS SNS Queues
* Reliable delivery mechanisms, that can be scaled out.
  * For example: Azure Functions
  * For example: AWS Lambdas 
* API workload to process data (for testability and load balancing).
  * For example: APIs 