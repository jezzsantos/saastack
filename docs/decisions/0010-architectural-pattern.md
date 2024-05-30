# Architectural Pattern

* status: accepted
* date: 2023-08-29
* deciders: jezzsantos

# Context and Problem Statement

We are providing a template of source code for a web-based backend and frontend for a web software product that will be accessed remotely over HTTPS.

It is web-based so that a small/growing product team can easily manage just one single version of the deployed software - to reduce support costs.

> As opposed to deploying multiple versions of an application (that runs on another platform) where the customer who buys a license to the software has full control over the deployment of the version of the software.

The software product is expected to:

* Evolve over a long period of time (1 - 10 years)
* Have many contributors (1 - 50), but initially, start with a small team (< 5)
* Change dramatically over time, especially in its first 3 years, and then at some point scale out when the business scales out

The software itself is expected to:

* Change constantly and be refactored continuously
* Evolved to suit the needs of the owning business
* Discover complexity (in the real world, it is modeling) that could not be predicted ahead of time
* Accumulate accidental complexity and technical debt over time
* Be split up into separate deployable units to scale out (and scale up)
* Performance Optimized in certain areas, as and when needed, once it is proven functional, stable and valuable to the business.

Managing complexity effectively (over time) is paramount to preventing the software from becoming brittle and preventing it from being changed efficiently over time. Particularly, when the people changing in the future were not the people designing it in the past/present.

## Decision Drivers

We believe that:

* There are critical advantages to a single team working on a single codebase when starting a new software business
* The most performant software components are deployed as a single unit (e.g. with shared memory, etc)
* Far too much complexity is introduced into a single codebase software component far too early in a product lifecycle, as a result of tight coupling, and under-engineering, while still discovering the domain they are modeling in the real world. This software is then not revised as a result of poor under-engineering practices, as there is no built-in regression testing capability. Largely due to the business driving more feature development in search of a product market kit.
* In the context of many new businesses (startups), teams must keep moving and making decisions, even if that means making suboptimal decisions so that they can learn more in the future to change their designs
* Eventually, the business will reach the point where it will need to reach a larger audience, and thus scale up (and out) its software economically

## Considered Options

The standard top-level architectural patterns today are:

* Modular Monoliths (discrete modules of a system in a single deployable unit)
* Monoliths (a combined system in a single deployable unit)
* Micro-services (a distributed system deployed in multiple deployable units)

There are other (undesirable/unsuccessful) variants of these:

* Distributed Monoliths

## Decision Outcome

`Modular Monolith`

For a small team, working on a new software system, they should take all the economic advantages of working together on a single deployable codebase, containing one or more compute units (web servers, databases, serverless triggers, message brokers, etc).

There are many advantages, not limited to:

* A single codebase containing all components, so that you can see all the pieces in one place
* The ability for tools to refactor across the codebase reliably (consistently)
* Evolve the domain language and terms in one place
* etc.

Monoliths as backends also accommodate some of these advantages. The key difference is that a Modular-Monolith is designed explicitly (from the start) to be split up into other monoliths later, whereas a monolith is not designed to be split later at all. (Conversely, it is either not designed at all for scale, or it is assumed to be replaced by something more optimal later). Neither is a reality for a tech startup without lots of capital investment, at critical growth stages.

Microservices represent the likely target architecture in the future for all products at large scale (some businesses never get to that scale), but they come with many costs and compromises that make no economic sense when you are starting with a small team. Particularly the fact that each microservice in the system is typically maintained by a separate team. Modular monolith is designed to evolve into several Microservices at some future date, but only once the economics can justify this level of scale. Particularly advantageous, once the domain has been well explored and has matured.

## More Information

TBA

> Here, one might want to provide additional evidence for the decision outcome (possibly including assumptions made) and/or document the team agreement on the decision (including the confidence level) and/or define how this decision should be realized and when it should be re-visited (the optional “Validation” section may also cover this aspect).
> Links to other decisions and resources might appear in this section as well.
