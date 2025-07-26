# Design Principles

[All Use Cases](0000-all-use-cases.md) the main use cases that we have implemented across the product (so that you do not have to implement them yourselves)

* [REST API Design Guidelines](0010-rest-api.md) how REST API's should be designed
* [REST API Framework](0020-api-framework.md) how REST APIs are implemented
* [Modularity](0025-modularity.md) is how we build modules that can be scaled-out later as the product grows
* [Recording/Logging/etc](0030-recording.md) how we do crash reporting, logging, auditing, and capture usage metrics
* [Configuration Management](0040-configuration.md) how we manage configuration in the source code at design-time and runtime
* [Domain Driven Design](0050-domain-driven-design.md) how to design your aggregates, and domains
* [Dependency Injection](0060-dependency-injection.md) how you implement DI
* [Persistence](0070-persistence.md) how you design your repository layer, and promote domain events
* [Eventing](0170-eventing.md) how we implement Eventing and enable Event Driven Architecture
* [Ports and Adapters](0080-ports-and-adapters.md) how we keep infrastructure components at arm's length, and testable, and how we integrate with any 3rd party system
* [Authentication and Authorization](0090-authentication-authorization.md) how we authenticate and authorize users
* [Email Delivery](0100-email-delivery.md) how we send emails and deliver them asynchronously and reliably
* [Backend for Frontend](0110-back-end-for-front-end.md) the BEFFE web server that is tailored for a web UI, and brokers secure access to the backend
* [Feature Flagging](0120-feature-flagging.md) how we enable and disable features at runtime
* [Multi-Tenancy](0130-multitenancy.md) how we support multiple tenants in our system (both logical and physical infrastructure)
* [Developer Tooling](0140-developer-tooling.md) all the tooling that is included in this codebase to help developers use this codebase effectively, and consistently
* [User Lifecycle](0160-user-lifecycle.md) how are users managed on the platform, and the relationship to their organizations
* [Billing Integration](0180-billing-integration.md) how the integration between the billing management and the product works to enable self-serve plan management 
* [Testing Strategies](0190-testing-strategies.md) how we test our codebase
* [Javascript Actions](0200-javascript-actions.md) a concept to enhance common user interface experiences 
* [Identity Server](0210-identity-server.md) what it is and how to use other providers 