[![Build and Test](https://github.com/jezzsantos/saastack-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/jezzsantos/saastack-dotnet/actions/workflows/build.yml)

# SaaStack .NET

Are you thinking of building a new SaaS product from scratch? (on .NET)

Then start with SaaStack. It is a complete "codebase template" for real-world, fully featured SaaS web products, that you can start with.

Ready to build, test, and deploy into a cloud of your choice (e.g. Azure, AWS, Google Cloud, etc)

>  This is not a starter EXAMPLE template of the type you would download to learn a new technology. 
>  This is a fully-fledged functional SaaS product that you can deploy from day one and get started building out your product with.

The codebase demonstrates common architectural styles, you are going to need in the long run, such as:
* [Modular-Monolith](https://www.thoughtworks.com/insights/blog/microservices/modular-monolith-better-way-build-software) - build a monolith first, the separate out to micro-services later
* [Clean Architecture, Onion Architecture, and Hexagonal Architecture](https://medium.com/@edamtoft/onion-vs-clean-vs-hexagonal-architecture-9ad94a27da91) principles - low-coupling, high-cohesion, a shareable domain at the center
* Host it behind a distributed REST API, in a CLI, or in another executable. 
* [Domain Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html) (and Aggregates) - Domain Modelling (behavior) over Data Modelling (data)
* [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) - because you cannot predict upfront when you will need historical data later 
* [Event-Driven Architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven) - to keep your modules de-coupled and asynchronous
* [Polyglot Persistence](https://martinfowler.com/bliki/PolyglotPersistence.html) - makes your entire system easy to test, and then select a more appropriate database technology later
* Extensive Test Automation (e.g. Unit, Integration, and E2E)
* B2B or B2C Multitenancy
* Extensibility for all integrations with any 3rd party provider (e.g. Stripe, Twilio, LaunchDarkly, etc) 

> The fundamental design principle behind this particular combination of architectural styles is to help defer making key (irreversible) decisions until later in your product development cycle, based on what you learn about building your specific product (which is realistically not predictable at the start of it). 

This starter template gives you most of the things all SaaS products will need from day one whilst maximizing your ability to evolve the specific behaviors and infrastructure components of your specific product - for the long run (i.e. over the course of the next 1-5 years).  

## Who is it for?

This starter template is NOT for everyone, nor for EVERY software project, nor for EVERY skill level.

* The people using this template must have some experience applying "first principles" of building new software products from scratch because it is a starter template that can (and should) be modified to suit your context. It is a far better starting point than building everything from scratch again.

* The tech stack is a .NET core backend (LTS version 6.0 or later) written in C#, using a few very popular and well-supported 3rd party libraries
* This starter template deliberately makes engineering trade-offs that are optimized for situations where:
  1. High maintainability is super important to you (e.g. long-lived codebases)
  2. Managing complexity over long periods of time is non-negotiable (~1-10 years), and avoiding big balls of mud (BBOMs) is paramount to you,
  3. Where many hands will touch the codebase (i.e. over the course of its entire life)

The kinds of '*known scenarios*' that this template is designed specifically for:
* Tech SaaS startups building their product from scratch
* or experienced developers who are very familiar with these patterns and concepts and wish to adapt them to their context

Can you use this template if your context is different?
* Yes, you can, but you need to be aware of why the trade-offs have been made in the way they have been made, then adapt them to your needs

Are these trade-offs suitable for any kind of software project?
* No, they are not. 
  * However, some of them may fit your specific context well.

> Want to know what the initial design constraints, assumptions, and trade-offs are, then see our [Decisions Log](docs/decisions/README.md) and [Design Principles](docs/design-principles/README.md) for more details on that.

## What does it give you?

It is a starter "template," not a 3rd party library or a fancy 3rd party framework:

* You copy this codebase, as is, as your new codebase for your product.
* You rename a few things to the name of your product.
* You compile it, you run its tests, and you deploy its pieces into your cloud environment (e.g. Azure, AWS, or Google Cloud).
* You then continue to evolve and add your own features to it (by following the established code patterns). You then evolve and adapt the code to wherever you need it to go.
  * Don't like those patterns? then change them to suit your preferences. There are no rigid frameworks or other dev teams to plead with.
* Read the [documentation](docs/README.md) to figure out what it already has and how things work.
  * So that you either don't need to worry about those specific things yet (and can focus on more valuable things), or you can modify them to suit your specific needs. It is your code, so you do as you please to it.


Since this starter "template" is NOT a framework (of the type you normally depend on from others downloaded from [nuget.org](https://nuget.org)), you are free from being trapped inside other people's abstractions and regimes and then waiting on them to accommodate your specific needs. With this template, all you need to do is understand the code, change the code to fit your needs, update the tests that cover it, and move on. Just like you do with all the code you write.

## Want it to scale?

What happens when the performance of this modular monolith requires that you must scale it out? 

> Remember: No business can afford the expense for you to re-write your product, - so forget that idea!

This codebase has been explicitly designed so that you can split it up and deploy its various modules into separate deployable units as you see fit (when your product is ready for that).

Unlike a traditional monolithic codebase (i.e. single deployable unit), all modules in this Modular Monolith codebase have been designed (and enforced) to be de-coupled and deployed independently in the future.

You just have to decide which modules belong in which deployed components, split things up, and you can deploy them separately.

> No more re-builds and extensive re-engineering to build a new distributed codebase when the time comes. It is all already in there for that future date.

## What does it contain?

It is a fully-functioning and tested system, with some common "base" functionality.

It demonstrates a working example of a *made-up* SaaS car-sharing platform, just for demonstration purposes.

> You would, of course, replace that stuff with your own product of course! It is only there to demonstrate real code examples you can learn from.

The starter template also takes care of these specific kinds of things:

* Deployment
  * It can be deployed in Azure (e.g. App Services or Functions) or in AWS (e.g. EC2 instances or Lambdas)
  * It is designed to be split into as many deployable pieces as you want when needed. (You simply replace the "RPC adapters" with "HttpClient adapters").
* REST API
  * It defines a ruleset about how JSON is represented on the wire and how requests are deserialized (to cope with different client styles)
  * It localizes developer errors
  * It handles and maps common exceptions to standard HTTP status codes
  * It returns standard HTTP statuses for successful requests based on the HTTP verb (e.g. 200, 201, 202, 204)
  * Provides a Swagger UI.
* Infrastructure
  * All infrastructure components are independently testable adapters
  * It implements multi-tenancy for inbound HTTP requests (e.g. HTTP Host headers, URL keys, etc.)
  * It implements multi-tenancy (for data segregation) using either data partitioning, or physical partitioning, or both.
  * It implements polyglot persistence, so you can use whatever persistence technology is appropriate for each module per data load (e.g. SQLServer, Postgres, Redis, DynamoDB, Amazon RDS, LocalFile, In-Memory, etc.)
  * It integrated 3rd party identity providers for authentication, 2FA, SSO, and credential management (e.g. Auth0, Microsoft Graph, Google, Amazon Cognito, etc.).
  * It integrates billing subscription management providers so that you can charge for your product use and determine feature sets based on subscription levels (e.g. Stripe, ChargeBee, Chargify, etc.).
  * It integrates feature flagging providers to control how to access your features and roll them out safely (e.g. LaunchDarkly, GitLab, Unleased, etc.)
  * It integrates product usage metrics to monitor and measure the actual usage of your product (e.g. MixPanel, Google Analytics, Application Insights, Amazon XRay, etc.)
  * It integrates crash analytics and structured logging so you can plug in your own preferred monitoring (e.g. Application Insights, CloudWatch, Sentry.io, etc.).
  * It uses dependency injection extensively so that all modules and components remain testable and configurable.
  * It defines standard and unified configuration patterns (e.g. using appsettings.json) to load tenanted or non-tenanted runtime settings.
* Application
  * Supports one or more applications, agnostic to infrastructure interop (i.e. allows you to expose each application as a REST API (default) or as a reliable Queue, or any other kind of infrastructure)
  * Supports transaction scripts + anemic domain model or Domain Driven Design
  * Applications are aligned to audiences and subdomains
* Others
  * It provides documented code examples for the most common use cases. Simply follow and learn from the existing patterns in the codebase
  * It provides a [decision log](docs/decisions/README.md) so you can see why certain design decisions were made.
  * It provides documentation about the [design principles](docs/design-principles/README.md) behind the codebase so you can learn about them, and why they exist.
  * It \[will\] provide an eco-system/marketplace of common adapters that other people can build and share with the community.
  * It demonstrates extensive and overlapping testing suites (unit tests, integration tests, and end-to-end tests) to ensure that production support issues are minimized, and regressions are caught early on. As well as allowing you to change any of the existing base code safely
  * It defines and enforces coding standards and formatting rules
  * It utilizes common patterns and abstractions around popular libraries (that are the most up-to-date in the .NET world), so you can switch them out for your preferences.
  * It defines horizontal layers and vertical slices to make changing code in any component easier and more reliable.
  * It enforces dependency direction rules so that layers and subdomains are not inadvertently coupled together (enforcing architectural constraints)
