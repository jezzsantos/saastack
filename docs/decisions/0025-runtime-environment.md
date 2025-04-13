# Runtime Environment

* status: accepted
* date: 2024-05-25
* deciders: jezzsantos

# Context and Problem Statement

As with all distributed cloud systems, there are many "units of compute" to execute software.
Examples of the "units of compute" cloud infrastructure are:

* Web Servers (or serverless components)
* Databases and data lakes
* Queues, and Message Buses

* IoT Devices, etc

These kinds of infrastructure are nowadays easy to integrate together using vendor SDKs, to compose functioning and reliable systems.

However, to interop with other systems (3rd party systems) today, the predominant protocol is synchronously with REST APIs over HTTPS.

This means that building REST APIs that are accessible over HTTP is a forgone conclusion to interop with 3rd party remote systems. But it also means that building REST APIs for internal composition of this distributed system is also very viable, especially in the early stages of the product.

For example,

Instead of dividing up your business rules (and subdomains) into spare components and deploying them to different hosts to run independently, you should find ways to configure the infrastructure so that this software executes behind (just another) API call.

That way we can define very robust architectural patterns for developing APIs (whether they are public on private) and maintain all our evolving rules in one place for as long as possible. We also get the benefits of scale-out load balancing.

> Later in the business, once the core of the product is stabilizing, when scaling up the organization and dividing up the product into separate independent teams, you can start to optimize and split up the API's and move code into different infrastructure to optimize throughput and performance.

A simple example,

In Azure, you can deploy domain rules/logic to run in a database in the form of a stored procedure, which is closer to the data it is manipulating. As opposed to running the domain rules/logic in your domain/application layer. You would do this to make use of some of the performance optimizations of these kinds of Azure SqlServer compute structures. This practice generally incurs heavy maintenance and support costs to the product team, for several reasons, including:

* Requiring specialized skill sets to change the stored procedures. T-SQL is not as common a programming language as say C#.
* Islands of domain rules/logic code in T-SQL stored procedures are not easily traceable by compilers and other development tools, making verifying them very difficult. This then requires intimate knowledge of how they work and requires an exhaustive end to and testing of them. Or it requires high levels of engineering rigor and process, to ensure that when code (that depends on these database structures) changes, these structures change as well in accordance.

> Stored procedures in databases are frowned upon nowadays as a general maintainable practice for all these reasons. Whereas back in the 1990's it was more commonplace, but nonetheless no-less problematic back then as it is today. We've learned better practices since then.

Another common example today, is in utilizing serverless components. For example, in Azure, many teams are using Azure Functions to provide handy little HTTP endpoints, that process varying workloads themselves. As opposed to including those HTTP endpoints in their main API, and shifting the workload over there. Motivations vary dramatically from case to case:

* Sometimes it is driven by general-purpose examples discovered by the developer online, ignorant of the larger architecture they are dealing with.
* Sometimes, it is because the developer is curious to master this new technology and give it a go, ignoring their larger architecture concerns.
* Sometimes, it is a genuine use case of integrating with other infrastructure components (like Data Lakes, EventHubs, Queues and Buses, etc.), but then they move all the processing code into the Azure Function itself as if it were a separate workload. Thus, ignorantly integrating seamlessly with other transactional infrastructure (like databases), as if they were sharable.

In all these cases, it generally just requires developers to step back and adopt a larger perspective on the architecture they are dealing with as a whole. And then to take the time and extra effort to use the serverless components to relay data to the APIs of their established subdomains, instead of spreading out the processing of the workload and mixing up concerns in less maintainable software, that does not evolve with the other software they are writing.

## Considered Options

As the unit of processing (with varied workloads), the options are:

1. APIs (and vertical subdomains)
2. Separately deployed islands of processing (to whatever cloud infrastructure native runtime).

## Decision Outcome

`APIs`

- The primary benefits of the consolidated patterns established in this codebase as a modular-monolith are exactly to make a changes to software easier and quicker and more reliable over time, for many people coming and going on the product team. Introducing bespoke patterns (optimized to specific cloud infrastructural runtimes) weakens that benefit dramatically in the long run. There is a time and place to do that, but that benefit comes much later in the lifecycle of the product. YOU should strive to keep it simple and cohesive (and de-coupled) for as long as possible.
- A workload becomes just another API (albeit a "private" API), which is just another use case declared and defined in the same place (cohesion) as all the other known ones. It can be easily refactored, changed or replaced at any time. It can be changed along with all the workloads that depend on it much more easily and reliably. Extensive regression testing (unit testing + integration testing + end-to-end testing) will verify the impact of any change made (early in the cycle), and ensure that it does not impact/break other known parts of the system. This is not so easy to achieve if the code is spread between multiple code repositories, or using different and independent testing harnesses.
