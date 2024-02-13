# Design Principles

* [REST API Design Guidelines](0010-rest-api.md) how REST API's should be designed
* [REST API Framework](0020-api-framework.md) how REST API's are implemented
* [Recording/Logging/etc](0030-recording.md) how we do crash reporting, logging, auditing, and capture usage metrics
* [Configuration Management](0040-configuration.md) how we manage configuration in the source code at design-time and runtime
* [Domain Driven Design](0050-domain-driven-design.md) how to design your aggregates, and domains
* [Dependency Injection](0060-dependency-injection.md) how you implement DI
* [Persistence](0070-persistence.md) how you design your repository layer, and promote domain events
* [Ports and Adapters](0080-ports-and-adapters.md) how we keep infrastructure components at arms length, and testable, and how we integrate with any 3rd party system
* [Authentication and Authorization](0090-authentication-authorization.md) how we authenticate and authorize users
* [Email Delivery](0100-email-delivery.md) how we send emails and deliver them asynchronously and reliably
* [Backend for Frontend](0110-back-end-for-front-end.md) the BEFFE web server that is tailored for a web UI, and brokers secure access to the backend
* [Feature Flagging](0120-feature-flagging.md) how we enable and disable features at runtime