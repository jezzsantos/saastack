# Persistence

* status: accepted
* date: {2023-12-03}
* deciders: jezzsantos

# Context and Problem Statement

Persistence is an essential aspect of any stateful or stateless cloud-based system.

When considering our chosen [Modeling](0040-modeling.md) strategy as: domain-driven-design, persisting state is no longer just about data-modelling and creating rows in tables in a relational database. DDD particularly is driven by handling domain-events rather than by snapshotting the latest state of aggregates in a system.

When considering multi-tenancy aspects, most SaaS systems require multi-tenancy, and in many cases, individual customers will not permit their data to be stored in the same physical databases/physical locations as other customers. Data sovereignty is a common issue.

When considering the best tool for the job, relational databases are not always the most optimal repositories of choice for some work loads, especially in the cloud. Today, there are many kinds of data storage available in the cloud, and many are optimized for specific work loads, given their locations and features. This is an important aspect of cloud economics especially for global SaaS products.

When considering the testability of a SaaS system, particularly on developers desktops/laptops, it is no longer feasible these days to install some cloud infrastructural components on a local laptop (emulators may also not exist), and starting so much infrastructure for debugging gets very hard very soon, especially when you have many deployment units that all need to run together to reproduce production environments.

## Considered Options

The options are:

1. Polyglot repositories - (right tool for the workload, logical/physical partitioning)
2. Single relational database - all data stored relational tables (e.g. SQLServer, Postgres or other)
3. Single NoSQL database - all data stored in documents/NV pairs (e.g. Azure Storage Account, DataLake, NoSQL or other)

## Decision Outcome

`Polyglot`

- Moving away from data-modeling means (to domain-modeling) developers need to stop thinking about relational databases and data optimization
- Best tool for the job - chose the appropriate store for the work load. i.e. events stored in an event store. read models in databases, temporary data in caches
- Testability - developers can work with in-memory databases on local machines, eliminating the need for emulators, or installing cloud infrastructure components in their machines
- Multi-tenancy - using dependency injection, and dynamic configuration, we can connect to different data sources specific to the subdomain and specific to individual tenants.

### (Optional) Consequences

Selecting polyglot persistence means crafting one or more abstractions of persistence (types of persistence) that can be adapted to each storage technology.
For example, domain state would be stored in an event store, that would need to define common aspects of all event stores.
For example, read model data would be stored in a traditional "record" stores like databases (Relational or NoSQL)

As with all abstractions they would need to define the lowest common denominator, in terms of capabilities, that all persistence technologies could implement.
This has the anticipated consequence that the generalized abstractions will not be specifically optimal for any specific technology implementation.

For example, a relational database will support optimization of queries based on specific workloads (e.g. indexes, stored procedures, views etc.). These are all RMDBS optimizations that are not supported in say NoSQL databases.

The thinking behind any abstraction that attempts to model all persistence technologies is optimizing for flexibility and testing, not optimizing for performance.
When performance of certain workloads is something that is critical to a system, then this abstraction can be replaced for more optimal implementations. And this should be feasible in the architecture.