# Modeling

* status: accepted
* date: 2023-09-10
* deciders: jezzsantos

# Context and Problem Statement

Writing software for long-lived SaaS products generally involves providing some kind of API to some kind of model of some kind of problem space - a *[conceptual model](https://uxdesign.cc/understanding-mental-and-conceptual-models-in-product-design-7d69de3cae26)* of the real world. 

> The API is the means to access the conceptual model, to write, and then read or change the state of the model over time. All conceptual models and modeled processes have some kind of lifecycle.

The conceptual model (and the related concepts from which is it composed) are normally described by names of things in the real world. 

>  In object-oriented programming, for example, the objects in the code are typically named after the real-world things or processes identified in the real world. 

These models are also known as abstractions, and as with all abstractions they only focus in on certain parts of the real-world things they are modeling. 

> Modeling all the details of the real-world "thing" is often too complex and too expensive to model. The real world has far too many facets and aspects to model accurately with any meaningful precision.

In its simplest form, a software "conceptual model" boils down to just a simplification of certain data and certain relationships, that together describe permutations that make up the state of the system at any time. Usually, decorated and protected with rules and constraints.

Traditionally, circa 1970s-1990s developers were taught to analyze, capture, and model the behavior of their software systems in terms of the smallest amount of data that could fit in a database, and then optimize that data for retrieval of the data. [ERD diagrams](https://en.wikipedia.org/wiki/Entity%E2%80%93relationship_model) were often the specifications (and documentation) used in that discipline. At that time, the cost of storing data was a major problem, and so was the [low] performance of searching through it all, given the available storage technologies available at the time. This gave rise to relational database technologies and normalization practices that had developers hyper-focused on cramming their data into the most efficient data types and efficient relationships to minimize the risk of concurrency errors and maximize the speed of searching across different data. It is also worth mentioning that, at that time, systems were not connected, and databases were expected to be used at 100% consistent with the software using them.

Today, things have changed dramatically, but developer practices are still lagging (unfortunately):

* Data now needs to be stored in distributed systems, not single instances of databases, on single machines. 
* 100% consistency is not always possible in any given system, and high availability is becoming more important. You cannot maximize both consistency and availability ([CAP Theorem](https://en.wikipedia.org/wiki/CAP_theorem)) at the same time.
* Storage of data is no longer expensive, thanks to the cloud and advances in silicon technology. Most databases today could actually be stored more affordably in RAM, rather than retrieved from physical disks, for maximum performance!
* Modeling systems in data and relationships has shown us over the last ~30 years that developers who practice data-modeling techniques tend to produce highly un-maintainable systems (big balls of mud) by trying to reuse data structures across a single database in large complex systems. Such systems are then too expensive to split up or scale later.

Real-world processes and problem domains, except the most trivial coding examples out there today, are far more complex to understand and to create robust conceptual models in software, that last the test of time. Especially, if the software is expected to change over the course of the next 1-20 years.

The sweet spot for using data modeling practices is when the conceptual model will be simple in design, and have little behavior to model. It is not a great fit for modeling complex systems. But it is a cheap tool to get initial software working.

Thus, for most long-lived software products, data modeling and CRUD-based designs are not fit for purpose, for designing most of the components of long-lived products. On examination of many stable, long-lived systems, data modeling was only appropriate for small ancillary parts of the system. However, knowing which components of a system are appropriate for data modeling before building those parts is not [in general] easy to predict ahead of time.

This realization alone makes data modeling a poor choice as a default starting point for complex systems.

Today, there are other proven disciplines and practices that put modeling the *observed* behavior of the real world ahead of modeling the assumed data behind it.  

Domain modeling is one such discipline.

## Considered Options

The options are:
1. Domain Driven Design (with Aggregates)
2. Data Modeling

## Decision Outcome

`Domain Driven Design`

- DDD focuses on identifying actual use cases/processes (observed in the real world) at any point in time and then adding/removing/refactoring more as they are realized. Data Modeling, on the other hand, attempts to guess (upfront) all the future possible states of the overall system,  since changing data types and relationships of the whole data model is extremely expensive to do (and support) later.

- DDD encapsulates all behaviors of the real world within the same code (e.g. within a single Aggregate) that derives the relationships and state from available data in a single unit of consistency. There is not necessarily a one-to-one mapping between the persisted data and the state of the aggregate. Whereas, in Data-Modeling, the data model provides no behavior nor constraints on what consumers can do with the data and how to change it. That is defined implicitly in one or more unrelated [transaction scripts](https://martinfowler.com/eaaCatalog/transactionScript.html) that operate over [anemic domain models](https://martinfowler.com/bliki/AnemicDomainModel.html). Over time, this means that identifiable use cases of the software must be derived just in time from reading (and understanding) code, rather than being explicit in the code. It also tends to breed duplication between transaction scripts and produces more production issues when the software is changed later since consumer behavior cannot be managed effectively by anemic domain models. 

  > In other words, the developers have no checks and balances in the code to help them make changes effectively and safely in the future, especially if they didn't write the original software themselves.

### Consequences

#### Good

1. Defining all use cases in one place (along with their invariants) in one place in the software is far easier to understand by all those working on changing the software, especially by those new to the software.
2. Not knowing that your persisted state is stored in any specific database technology allows developers the freedom to focus on the design of the software specific to their context, rather than trying to guess the ideal and generalized data model. The persistence of the state of an aggregate becomes the concern of a single component in the system, and that can be changed independently from the aggregate. That state can then be stored in any number of actual database technologies, and then optimized later for high performance.
3. Not all state is required to be stored in relational databases. 
4. The state of different aggregates should always be logically and physically segregated from each other. There should be no dependencies between aggregates, either binary dependencies or shared persisted state. Unlike most data-modeled systems in the real world, where it is a common practice for developers to share the data in database tables between unrelated components in the system, to save initial development time. This is forbidden in distributed systems.
5. This data segregation allows the possibility of choosing more appropriate persistence technology per aggregate. 
6. Moving from DDD to Data Modeling is trivial work to perform, once the aggregate behavior is known and has stabilized later in the development of the software. Going from Data Modeling to DDD, requires much more cost in engineering work since all transaction scripts must be understood and untangled together (often they have tangled dependencies between them). This engineering work often reveals previously unrealized concepts the software (hidden in implicit relationships), and the work to refactor that in itself expands the design and causes expensive and complex re-designs.
7. DDD mandates that persistence is not the concern of the domain, and thus, this kind of software tends to be more testable than the typical transaction scripts used in data modeling, which often depend on database technologies (e.g. ORMs and database clients) to do their work.

#### Bad

1. It requires more development work and discipline to encapsulate rules and data into aggregates than it does to build CRUD structures over relational databases (especially with the tooling and automation available for later today).