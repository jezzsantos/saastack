# Front-End Integration

* status: accepted
* date: 2024-02-10
* deciders: jezzsantos

# Context and Problem Statement

In SaaStack, we are demonstrating a template for a typical secure SaaS web product.

Most SaaS products today start out as only web applications (to achieve a single deployed version of a product). But later, there are always reasons to offer an API platform for integration into other products. Thus, SaaStack is demonstrating both from day one. In some SaaS businesses, other kinds of applications are also pursued, for example, Mobile apps, IoT devices, Desktop apps, integrations to marketplaces. In all these cases, a central Backend API is desirable at all stages of the business.

Backend APIs (typically REST APIs) are designed for two things:

1. To model complex business workflows (as opposed to what is more common to most developers, just modelling a relational database)
2. To be consumed by human developers building integrations to 3rd party systems, or other applications.

Frontend applications are designed for one thing:

1. To provide an optimized human or machine interface that interacts with a core product (i.e., a Backend API)

Every Frontend application has a discrete set of users (human or machine) and a discrete subset of use cases to expose.

Every Frontend application requires it to be optimized for usability on a specific platform. (viz: the difference between a web app and a mobile app).

No Backend API (collectively) can be optimal for each and every Frontend that integrates with it, but it can provide a superset of use cases for most Frontend applications that integrate with it.

Thus, it becomes necessary for a more optimized translation between what data and performance can be provided by a Backend and what is required by any specific Frontend.

That translation can be done in one of 3 places:

1. In the centralized Backend, exact to each remote Frontend, defined by the remote Frontend on demand (i.e., multiple data and performance profiles in the same place, that change whenever a Frontend is added or changed)
2. In the remote FrontEnd.
3. In a dedicated "nearby" intermediary between the centralized Backend and remote Frontend. (i.e., may change whenever a specific Frontend changes)

> When it comes to the web and remote distributed systems, capacity, performance, and reliability are not guarantees. See the [Fallacies of Distributed Computing](https://en.wikipedia.org/wiki/Fallacies_of_distributed_computing). Thus, any communication between components in a distributed system is going to compromise the performance and reliability of any remote system.

## Considered Options

The options are:

1. One centralized Backend, several "nearby" dedicated BEFFE + one remote Frontend
2. One centralized Backend, several remote Frontends (i.e., no intermediaries)
3. One remote Frontend, one Dedicated centralized Backend (i.e., no standalone API)

## Decision Outcome

`BEFFE`

- Every application type has its unique needs that a dedicated BEFFE can accommodate on-demand, without changing any Backend.
- A BEFFE is always deployed "nearby" to the Backend where they don't pay the network performance compromises that remote Frontends incur
- BEFFE can cache, aggregate, and filter data for a specific use-case in a Frontend, that would otherwise require too much data or latency if delivering from a Backend.
- A BEFFE can cache, aggregate and filter data from one or more Backend API or 3rd party systems to save on chatty communications with so many systems, and deliver just what the Frontend desires, how it desires it, in its most optimal form.
- Authentication/Authorization can be performed appropriately for each kind of Frontend by the BEFFE, rather than incurring that complexity in a Backend. Separation of concerns. For example, using secure cookies for untrusted Frontends (e.g. JavaScript apps), and tokens for trusted machine Backends.

## (Optional) More Information

See these strong arguments for distributed systems:

* https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends
* https://samnewman.io/patterns/architectural/bff/
