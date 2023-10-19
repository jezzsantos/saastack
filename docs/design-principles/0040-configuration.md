# Configuration

## Design Principles

1. We want all components in each host to have access to static configuration (read-only), that would be set at deployment time.
2. We want that configuration to be written into packaged assets just before deployment, in the CD pipeline.
3. We want that configuration to be specific to a specific environment (e.g. Local development, Staging, Holding or Production)
4. We do not want developers writing anything but local development environment settings (secrets or otherwise) into configuration files. With one exception: the configuration used to configure components for integration testing against real 3rd party systems (i.e. in tests of the category: `Integration.External`). These 3rd party accounts/environments, should never be related to production environments, and are designed only for testing-only. Configuration (and especially any secrets) used for these accounts/environments can NEVER lead those with access to them to compromise the system or its integrity.
5. We will need some configuration for the SaaS "platform" (all shared components), and some configuration for each "tenant" running on the platform. These two sets of configuration must be kept separate for each other, but may not be stored in the same repositories. (e.g. platform configuration is defined in appsettings.json, whilst tenancy configuration is stored in a database)
6. Configuration needs to be hierarchical (e.g. namespaced), and hierarchical in terms of layering.
7. Settings are expected to be of only 3 types: `string`, `number` and `boolean`
8. Components are responsible for reading their own configuration, and shall not re-use other components configuration.
9. Secrets may be stored separately from non-confidential configuration in other repositories (e.g. files, databases, 3rd party services).
10. We want to be able to change storage location of configuration at any time, without breaking code (e.g. files, databases, 3rd party services).
11. We want to use dependency injection to give components their configuration.

## Implementation

The `IConfigurationSettings` abstraction is used to give access to configuration for both "Platform" settings and "Tenancy" settings.

### Platform Settings

Platform settings are setting that are shared across all components running in the platform.

For example:

* Connection strings to centralized repositories (for hosting data pertaining to all tenants on the platform)
* Account details for accessing shared 3rd party system accounts via adapters (e.g. an email provider)
* Keys and defaults for various application and domain services

Most of these settings will be stored in standard places that are supported by the .NET runtime, such as `appsettings.json` files for the specific environment.

### Tenancy Settings

Tenancy settings are setting that are specific to a tenant running on the platform.

For example:

* Connection strings to a tenant's physically partitioned repository (e.g. in a nearby datacenter of their choice)
* Account details for accessing a specific 3rd party system account via adapters (e.g. an accounting integration)

At runtime, in a multi-tenanted host, when the inbound HTTP request is destined for an API that is tenanted, the `ITenantContext` will define the tenancy and settings for the current HTTP request.

These settings are generally read from a dynamic repository (e.g. a database, or 3rd party service), and they are unique to the specific tenant.

> Never to be accidentally accessed by or exposed to other tenants running on the platform