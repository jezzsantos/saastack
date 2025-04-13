# Eventing

Eventing is the gateway to Event Driven Architecture (EDA), but it starts in the Domain Layer of the software, with domain events.

When we refer to "Eventing" we are describing only the communication of information between different parts of a [distributed] system.

> Event-sourcing is not considered to be the same as eventing, especially since it involves domain events also. But event sourcing is about persistence, it is not about communication.

## Design Principles

1. We want all DDD aggregates to utilize "domain events" to drive use cases. Irrespective of whether we they are persisting their state from events (a.k.a. Event Sourcing) or sourcing/capturing their current state from data snapshots (i.e. traditional record persistence in database tables).
2. For [event-sourcing persistence schemes](0070-persistence.md) (that are, by definition, "write models" only), we need to build associated "read models" so that we can query across multiple domain aggregates.
3. We want the flexibility to add/change our "read models" at any time, as the software changes, and not have lost any original data, and ideally re-create views of the data that are not previously "seen" (nor persisted) in the past.
4. We want to prioritize denormalized views of data to query for maximum efficiency (i.e., without requiring complex joins, only necessary for optimizing the writing of related data).
5. We want to de-couple subdomains from each other as much as possible, both technical decoupling, and semantic decoupling. Even for subdomains that are highly-coupled to begin with (e.g., `EndUsers` and `Organizations` and `Subscriptions`).
6. We want to deploy certain groups of subdomains into separate hosts and split the modular monolith into many independent hosts (a.k.a., microservices) much later in the lifecycle of the product. When we do, we do not have to re-engineer flows to accommodate those changes.
7. Sending imperative "commands" (i.e. API calls) between different API's is by its nature unreliable, and it encourages coupling (and assumptions) in many subtle ways for the software designer. We want to try to minimize that.
8. We want the flexibility to make changes to key use cases in the product, without changing multiple subdomains at the same time.
9. We want to be able to communicate across process boundaries without coupling the processes.

## Implementation

In the design of most distributed systems, of the nature of that kind of system (or of systems that are expected to evolve into distributed systems later) it is common practice to decouple each of the subdomains from each other.

De-coupling effectively, is absolutely vital to allowing the system to change independently, grow, and evolve over time quickly and reliably.

> Decoupling is the whole point of having a single direction of dependencies, from: Infrastructure components -> Domain components. Coupling is the primary cause of the "accidental complexity" that software designers put into their code, in small incremental steps.

Lack of effective de-coupling (at the technical level) is the main reason most software systems devolve into big-balls-of-mud, simply due to the accelerated coupling of many components to many other components, driven by the desires to maximize data and code reuse.

> Code re-use is a useful technique to combat the problem of not having to update multiple places in the code/system that share the same precise behaviour/context. But this technique (and the blind pursuit of it) is also the primary cause of coupling when components, that have distinct bounds and behaviour, are coupled when their behaviour/context eventually diverges.
>
> Knowing when to reuse and not reuse is an important skill to develop over years. Being DRY is never the goal for everything, it is a precision tool that needs to be applied carefully. So does decoupling with some duplication.

There are several fundamental techniques for de-coupling your codebases, including:

1. Separating code into horizontal layers, with well defined concerns (i.e. Infrastructure, Application, and Domain).
2. Separating subdomains into Vertical Slices.
3. Using ports and adapters and domain specific abstractions.
4. Starting with a modular monolith, and then decomposing it into separately deployable units (a.k.a., microservices) later, when you need to scale out.

Another technique is the use of Event-Driven Architecture (EDA), where change is communicated within process boundaries, AND across process boundaries.

EDA relies on the fact that your system will emit "domain events" that it can share both within specific bounded contexts (as "domain events"), and externally to other systems (as "integration events").

> When sharing events within a bounded context (or within the same process), the process can remain consistent. We call these "domain events." When events are shared across bounded contexts (or across processes and hosts or systems), they are called "integration events".
>

In this architecture:

1. We use "domain events" to signal changes (within the Domain Layer) and within all aggregates and entities.
   1. Regardless of whether we are using event sourcing for persistence or not.

2. We publish all "domain events" whenever the state of any aggregate is saved in any repository via the `EventSourcingDddCommandStore` or via the `SnapshottingDddCommandStore`.
3. We treat "domain events" and "integration events" slightly differently:
   1. "domain events" are published synchronously and handled synchronously after the aggregate is saved, and are always consistent.
   2. "integration events" are published synchronously, but are expected to be handled asynchronously (by a message broker) and be eventually consistent.

> We assume that all "domain events" are only ever published to other subdomains that are in the same "bounded context" and, thus, we also assume the bounded context in deployed in the same host process. When this is not true, for example, if subdomains of the same bounded context are split into separate host processes, then these subdomains will need to communicate with "integration events" instead, and they will necessarily become "eventually consistent" with each other.

![Eventing](../images/Persistence-Eventing.png)

The diagram above illustrates the "logical" process that is triggered when an aggregate state is updated.

> The implementation details of this "logical" process can be different depending on the specific "relay" mechanisms in place. See the [runtime](0170-eventing.md#runtime) details later.

### Reliability

Read model projections and notifications are created by publishing "domain events" after being raised by aggregates, from a specific subdomain.

In a typical stateless API (of the kind we have here), the publication of these events, and the subsequent updating of read models and notification handlers can occur synchronously or asynchronously with respect to the aggregates producing the "domain events" in a specific subdomain. The important part is that they reliably occur (for all subscriptions), and are eventually synchronized across the system.

Projections and Notifications SHOULD try to be as "consistent" as possible with updating the aggregates that produce the change events as much as possible, although "eventually consistent" is more than likely, and eventually, is far more common in distributed systems.

Consistency requires "reliable" implementations (e.g., Outbox Patterns), and these implementations must guarantee the delivery of the "domain events" in order (e.g., FIFO Queues/Buses). If either of these technical requirements cannot be guaranteed, then there is a high probability that when the system comes under load or stress, downstream consumers of these change events will be permanently out of sync, affecting the data integrity of downstream dependent systems. Which is very hard to diagnose and repair.

> Note: It is possible, with lots of effort, to design systems that can be tolerable to out of order delivery and unreliable delivery. However, at present the requirement for this architecture is FIFO and In Order delivery.

### Eventual Consistency

Eventual consistency can cause problems for clients who are making changes in synchronous "commands" and then shortly after, expecting that changed data in subsequent "queries" is immediately up to date. Depending on timing, this consistency is not-guaranteed.

By default, the mechanism of updating read models should be done reliably and *asynchronously* after a source aggregate is changed, such that the collective system is "eventually consistent". This asynchronous update (typically expected to take anywhere between ~100ms-5s) means that read model data and consumers of notifications can be "immediately" out of date with the subdomains that update their source aggregates.

> When this update process is synchronous (and also in-process), that part of the system will achieve 100% consistency, which is convenient, but this is not a true reality for when the system is eventually split up and becomes a distributed system. (as is the goal of all modular monoliths).

In distributed systems that are inevitably "eventually consistent", API clients are required to employ different strategies to handle this eventual consistency, which can be disruptive and problematic when switching later when a monolithic backend becomes distributed, and clients are expecting data that has not changed yet.

> For example, if a client calls a command API and then after receiving a response, immediately calls a query API that would include the changed data, the queried data may have not yet been updated yet. This is one reason why commands can return changed data synchronously in their responses, to help clients predict the changed data, and avoid the subsequent querying of it.

Because of this constraint, it is better to start the modular monolith on an eventually consistent model rather than start a fully consistent model since these client strategies should be established sooner rather than later being re-engineered.

### Event Propagation

With the addition of Eventing as a communication mechanism, and the need to handle either "domain events" or "integration events" published from other subdomains, in the context of a modular monolithic backend, we now have two ways that data can be exchanged between different subdomains.

1. **Imperatively**: in the direct request/response of a synchronous HTTP request of a REST API, as a command, or
2. **Implicitly**: In the data of an "integration event" or "domain event" delivered asynchronously to us via some external Message Broker.

Either mechanism is viable. Each one comes at a different cost.

As we do with all asynchronous workloads that use FIFO queues, and due to [this design decision](../decisions/0025-runtime-environment.md), we have chosen to rationalize all of these kinds of workloads, ultimately, into API calls.

> Note: With message broking, as opposed to FIFO queues, we typically have to manage the delivery of a single "domain/integration event" to one or more consumers. Whereas, in FIFO queues we only expect one consumer.

This means that for any message broker infrastructure, we expect the message to be delivered via an API call to a specific deployed ApiHost (i.e. an IP address), as opposed to making an API call to a unified (load-balanced) endpoint (i.e. a URL).
Then, this API call (`EventNotifications`) can be handled by any number of registered `IDomainEventNotficationConsumers` (in the API process) and routed to (one or more) Application Layers for further processing.

In Azure, for example, this can be achieved with an Azure function trigger set on an Azure Service Bus topic with a specific subscription.

> As with all FIFO queues (Azure Storage Queue + Azure Function + Trigger), this specific API call will also be "private".

![Azure Brokering](../images/Eventing-Brokering-Azure.png)

In AWS, for example, this can be achieved with a Lambda trigger set on an SNS topic with a specific subscription.

> As with all FIFO queues (AWS SQS queue + Trigger), this specific API call will also be "private".

![AWS Brokering](../images/Eventing-Brokering-AWS.png)

### Read Model Projections

A "Read Model" is the term used to describe data that is readable, usually by a query of some kind.

A "Projection" is a word used in databases to "project" (verb) data from one container into a specific view of that data.

> For example, in SQL databases, all `SELECT` statements represent projections of data into an output.

Read Model Projections are essentially views of data produced by projecting "domain events" onto a data container. In most cases, projecting data into the rows of tables of a database, optimized for fast querying.

Read model "projections" are a mechanism to produce (and keep up to date) one (or more) "read models", which are typically "records" that represent the latest state of the aggregates and entities in event-sourced persistent schemes. These read models are typically used directly by CQRS queries to retrieve the latest state of the system.

> Note: In snapshotting persistence schemes, the "read models" already exist, and are the exact same as the "write models"; they share the exact same data. However, there is no "read model" in an event source scheme, and "write models" cannot be efficiently queried.

One major advantage of producing "read models" is that they are all built from the historical stream of events. This means that we can have several of them at the same time, containing different data and all coherent with each other, unlike what is possible with snapshotting stores, with one and only one read model possible.

Another advantage of having read models is that we can build several "denormalized" sets of records at the same time (e.g. in a relational database) that are optimized for specific queries - no longer requiring complex joins to give desired results.

Another advantage (only available to event-sourced persistence scheme) is that we can rebuild any read model at any time, in any way we like, and never lose any historical data. Now that this is possible, read models become temporary and disposable (since we can rebuild them in any representation at any time). All the source data is in the event streams! The advantage here is that when the software changes and the queried data needs changing, we can use any of the historical data that already exists in the aggregate event streams to rebuild different data in completely new read models. No longer do you need to protect the one and only read model that defines the state of the system.

> This capability is impossible in snapshotting persistence schemes.

### Event Notifications

Notifications are the mechanism by which subdomains can communicate to other subdomains (or to other processes) about what is happening in the source subdomain. This means that a source subdomain does not have to [imperatively] instruct another [dependent] target subdomain to update its state, when the source subdomain state changes. Typically, this is done by a direct synchronous method/API call.

Instead, the target domain can simply "observe" and react to the appearance of a "domain event" from the source subdomain, and take appropriate action.

The coupling of the imperative method/API call is eliminated.

> This is particularly useful when you have highly inter-dependent subdomains, that require that their data be in sync with each other (i.e., `EndUser` memberships with `Organizations` and `UserProfiles`). As seen below.

![Generic Subdomains](../images/Eventing-Flows-Generic.png)

This eventing capability is particularly necessary in distributed deployments, where direct calls between separately deployed components are realized as HTTP calls (requiring both the source and target subdomains to be synchronously responsive and consistent to each other).

Instead, decoupling this asynchronously via "integration events" would normally be done in distributed systems with a message broker of some kind (i.e., a queue, a message bus, etc.).

The synchronous publication of all "domain events" is handled automatically by the `IEventNotifyingStoreNotificationRelay` (after events have first been projected by the `IEventNotifyingStoreProjectionRelay`).

Domain/Integration events are published synchronously (round-robin) one at a time:

1. First, to all registered `IDomainEventNotificationConsumer` consumers. These consumers can fail and report back errors that are captured synchronously.
2. Then to all registered `IIntegrationEventNotificationTranslator` translators, that have the option to translate a "domain event" into an "integration event" or not. This translation can also fail, and report back errors that are captured synchronously.
3. Finally, if the translator translates a "domain event" into an "integration event" it is then published to the `IEventNotificationMessageBroker` that should send the "integration event" to some external message broker, who will deliver it asynchronous to external consumers. This can also fail, and report back errors that are captured synchronously.

