# Web Interop

* status: accepted
* date: 2023-09-09
* deciders: jezzsantos

# Context and Problem Statement

In a web application (i.e. one deployed in the cloud) there needs to be an easy way that remote clients can interop with the centralized system (regardless of whether it is a monolith or distributed).

JSON over HTTP is the standard interop format today.

* REST is the standard way to provide behavioral-driven systems, where we model actual real-world processes as use cases for the software. In other words, we model the state-machine of an ongoing process (sometimes completed immediately, sometimes long-running).

* [OAR](https://mooreniemi.github.io/rest/apis/2016/11/08/oar-is-not-rest.html) (Object as Resource) is the traditional way of modeling CRUD systems that simply expose a database, and permit the client to decide the behavior of the system.

Web interop via an API is just one way to provide a public interface to an application.

A system may include it or others, such as:

* CLI (Command Line Interface) - direct connection to a remote application using some protocol (could be proprietary or standard)
* Reliable Queues - passing messages to queues monitored by the application
* Service Buses - passing messages to buses that consume messages

## Considered Options

The options are:
1. REST
2. OAR

## Decision Outcome

`REST`

- SaaS systems are expected to be long-lived and will involve incremental increases in complexity over time, they must exceed in usability to be effective with larger audiences. To be more generic and highly usable requires that remote clients have a well-known, and highly constrained interop interface to work with.   
- DDD and other architectural styles used in the application layers, also rely on modeling behavior rather than just data, so REST is a better external interface for them.

### Consequences

Most developers are well-versed in OAR patterns. They are a fundamental building block in any developer's competence.

It is often the case that the data entities used in OAR systems correspond directly to tables in an SQL database, which makes the systems quicker to build. But this developer [premature] optimization is rarely a saving for more complex systems. 

The OAR pattern has real-world problems for long-lived products - and generally over time produces BBOM (big balls of mud), as [transaction scripts](https://martinfowler.com/eaaCatalog/transactionScript.html) and [anemic domain models](https://martinfowler.com/bliki/AnemicDomainModel.html) tend to add more accidental complexity over time, as the system is built out.

The challenge with choosing REST is applying the discipline to continuously evolve the API towards behaviors (use cases) of the system, rather than designing them based on data. This is a new discipline that developers need to master and enforce over time.

## More Information

REST does not just mean JSON over HTTP.

REST defines "resources" that are involved in processes, that model the real world. They are not just defined by nouns modeled in SQL databases. In fact, it is possible that the REST resources are converted into other domain entities further down the stack in the domain model, and further that data entities in the database are different as well.



Designing REST requires a deeper insight into remote client use cases. Read our [guidance on designing REST APIs](../principles/0010-rest-api.md)
