# Managing Coupling

> The title assigns a {name} to the AD to be identified and searched for efficiently. 
> Ideally, it should convey the essence of the problem solved and the solution chosen.

* status: accepted
* date: 2023-08-09
* deciders: jezzsantos

# Context and Problem Statement

When building any software system, the complexity of the system in the future will be unknown to those building it at the time they build it. 
That \[future\] complexity will be only discovered as they build the system over time. It is not predictable

However, we can say that the amount of complexity introduced into the software ([accidental complexity](https://en.wikipedia.org/wiki/No_Silver_Bullet)) is heavily influenced by how much software is written over time (e.g. how many features, use cases, etc): 
* Short-lived software projects tends to be less complex than longer lived software products.
* We can also say that any software product, driving the growth of any sustainable business (e.g. a SaaS startup), is going to live for years, if not decades. Whereas, pet software projects, prototypes, etc tend to have a shorter lifespans, and thus less complexity.

If an increase in complexity is not considered or managed well from day one (e.g. either the developers don't know how to do that \[unskilled\], or they are deciding to ignore or prioritise other things), technical debt in the form of 'accidental complexity' will build rapidly in the code - even over short periods of time (< 12 months). 
* This accumulation continues for long periods of time, especially when teams have many developers touch the code and have no governance applied.
* This accumulation inevitably slows each developers' ability to change the software, to make even simple fixes to it. 

The primary cause of this difficulty in changing code (a.k.a legacy code) is generally to do with coupling components together, such that, changes to one component affects many others - sometimes in unexpected or detrimental ways.

In most software systems, there are things that change in the infrastructure of the software (e.g. IO components, like UIs and database structures) that will be optimized over time, due to different choices, scaling factors or changes to technologies. And there are things in the core behavior of the system (e.g. defined by the current understanding of the domain being modelled by the software) that also change over time (a.k.a business rules). 

However, the rate of change of these two related kinds of changes is quite different, and if not separated and contained appropriately, a change in one area may dramatically affect the other. Often to the detriment of the whole system. The code then becomes hard to change, its becomes brittle, and changing the code is prone to financially damaging the business. 

As an example. Developers should be able to change the user interface of an application without breaking any core domain behaviours.

Coupling components together is primary contributor to creating this kind of accidental complexity. And this coupling needs managed effectively over long periods of time.

## Considered Options

The common architectural proposals that are well-known are:

1. Hexagonal Architecture. a.k.a Ports and Adapters (Alistair Cockburn, 2005)
2. Clean Architecture (uncle bob circa 2012)
3. Onion Architecture (Jeffery Palermo, 2008)

Each of these proposals manage coupling by putting the core/domain of the software at the center of the architecture, and have the IO/infrastructural components on the peripheral.

Dependencies are managed by having the core/domain components define interfaces/contracts that are implemented by IO/infrastructural components. Thus the components in the "infrastructure" layer depends on the "core-domain" layer. No dependencies must be taken by the core/domain layer on other layers.

## Decision Outcome

`Ports and Adapters`
* Ports and Adapters (like the other proposals) is a simpler conceptual model for developers to understand.

In fact, choosing any of the options does not really change the outcome. It only changes the language we use to describe the components.

'Ports and Adapters' seems to be (from common use in the field) a more useful ways to describe to implementation details of these things to developers. As opposed to using the more abstract terms (and lesser concrete languages) used by Onion and Clean.

'Ports and Adapters' is also a less familiar term from other concepts that are often overloaded, like: "Service Interface" which have been introduced by other vendors, frameworks etc.

It is pretty easy to picture these concepts in code, by saying something like: 
* "an adapter plugs into a port", and,
* a "port" is implemented as a C# interface, an "adapter" is simply a concrete class that implements that C# interface.

> Even though we would not use these terms "port" nor "adapter" in any code identifiers in the code.

## More Information

See this [brief overview](https://medium.com/@edamtoft/onion-vs-clean-vs-hexagonal-architecture-9ad94a27da91) of the differences between the different architecture proposals, and then navigate these links to the original works:
1. [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
2. [Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/)
3. [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)