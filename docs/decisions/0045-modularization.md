# Modularization

* status: accepted
* date: 2023-09-17
* deciders: jezzsantos

# Context and Problem Statement

A key quality of a [Modular Monolith](0010-architectural-pattern.md) codebase is that you have discrete "modules" that can be re-deployed in one or more hosts, with little cost in time and engineering.

> For example, splitting one service into two, as separate services or containers in the cloud.

These modules will need to remain decoupled from one another (from top to bottom), and not share services or the data of other modules.

That means that they must have a well-defined interface to interop with each other, and they must maintain their autonomy with their own data and state.

We have chosen to make [REST the interop mechanism](0030-web-interop.md).

We have chosen to use [Domain Driven Design](0040-modeling.md) as the tool for capturing and encoding our "conceptual models" of the world to align with the mental models our customers have, of the problems we are trying to solve for them.

We have also chosen [Ports and Adapters](0020-managing-coupling.md) (implies a Clean, Onion, Hexagonal architecture) that define discrete layers with the core domain at the center, and infrastructure at its extent.

With this combination of architectural styles, our "modules" are going to need to be horizontally layered. e.g. Infrastructure, Application, Domain, but they will also need to represent vertical slices of functionality.

The question is, what represents the bounds of a deployable module?

## Considered Options

The options are:

1. DDD Subdomains
2. REST resources (Data entities)
3. Audiences (very coarse) - certain market segments, user types, etc
4. Use-cases (very granular)
5. Workload Types

## Decision Outcome

`DDD Subdomains`

- At the start of building a SaaS product, the bounded contexts of the software are largely unknown, but what is known are the use cases being designed for in the early days. Subdomains will naturally evolve from grouping those use cases around specific aggregates/resources, as they are explored and built out.

   - > Bounded contexts, at the beginning of a software build, pretty much represent a whole subdomain itself. More sophisticated definitions of bounded contexts be derived far later in the journey, when more about the domain is learned.

- REST resources are designed to expose a remote HTTP API to remote consumers. It is not the only way to interop with a SaaS product. Service buses, event hubs, queues, IoT, and other infrastructure are also interop mechanisms. REST resources are designed around real-world processes and use cases, so the overlap is close to aggregates and subdomains, but not precisely the same.

- Use-cases (in general) are far too granular as separate deployment units to warrant the cost of separate infrastructure, for the cases where a SaaS business has small teams in the early days of a start-up. This level of granularity may be required later, once the business has scaled beyond a small team(s).

### Consequences

Choosing DDD Subdomains at the early stage of a SaaS business is a good starting point, and there is nothing to stop a subdomain from splitting (or converging) as the system is explored and built out. In fact, this divergence and convergence for each subdomain is anticipated, to realize eventually reach bounded contexts.

Thus, DDD aggregates (as containers of use cases) will be expected to change and evolve over time, with deeper understanding of the entire system.

Therefore, making this choice early does not preclude changing it later when the conditions of the business change to warrant choosing another vertical slice definition.
