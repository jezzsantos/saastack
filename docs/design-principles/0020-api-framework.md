# Web Framework

## Design Drivers

1. We want to leverage core supported Microsoft ASP.NET facilities, rather than some other bespoke framework (like ServiceStack.net).
2. We are choosing Minimal API's over Controllers.
3. We want to model Requests and Responses that are related, easy to validate and in one place the code, We desire the [REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern).
4. However, the standard Minimal API patterns are difficult to organize and maintain in larger codebases.
5. We want a design that is easier to define and organize the API into modules, but yet have them be realised as Minimal APIs.
6. We want to have automatic validation, as whole requests
7. We want to [MediatR](https://github.com/jbogard/MediatR) handlers, to make Minimal API registration and dependency injection easier

### Modularity

One of the distinguishing design principles of a Modular Monolith over a Monolith is the ability to deploy any, all or some the API's in any number of deployment units. Taken to the extreme, you would end up with granular microservices. But smaller steps are very acceptable depending on the stage of the SaaS product.

The ability to deploy any (Subdomain) of the code to a separate web host, should be quick and easy to accomplish.

One of the things that has to be easy to do, is to register who the endpoints of a subdomain in whatever host you like, as well as all its dependencies.

With minimal API's there should be a modular way of registering both its endpoints and handlers, and then moving them to other hosts later.

### Organisation

The design of Minimal API's makes developing 10s or 100s of them in a single project quite unwieldy. They certainly would not live in one file.

Since they are registered as handlers, there is no concept of groups of API's. Whereas many API endpoints are naturally grouped or categorized. This is certainly the case when exposing subdomains.

When using MediatR to register handlers for minimal API's, and with dependency injection, it becomes quite tedious and repetitive to write a handler class for every route, when many routes are grouped and will be sharing the same dependencies.

There are better ways to organize these groups of endpoints into classes, and test them more easily.

### Validation

When you design endpoints you want the requests and responses to be coupled, and you want the requests to be validated automatically when requests come in. Writing wiring code for validation is lso very tedious and error prone, and so is writing code to response with errors in a consistent manner.

We want the codebase to make validation easier to do, and apply it automatically and have standard ways to report errors detected by it.

## Configuring API's

All APIs will be defined in a separate project that is initially part of a subdomain group of code. That project can then be registered as a module into a specific web host, and with it all the endpoints, handlers, dependencies needed for all layers of the subdomain.

The web host, will then code generate the endpoint declarations and handlers and register them with Minimal API, and other components can be registered with the IoC.

### Reference the Source Generator

Every API project must reference the Source Generators in `Infrastructure.WebApi.Generators`.

EveryAPI must provide a plugin.

The plugin will then automatically call the source-generated registration code, update the runtime configuration of the web host and populate the IoC automatically.

The configuration of the web host and its features will be encapsulated and provided by various extension methods, so that all API hosts are consistent. 