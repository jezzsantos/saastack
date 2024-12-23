# Configuration

Every SaaS system is driven by configuration.

In order to run these systems is different environments (e.g., `Local`, `Staging` and `Production`), different configuration is required, so that we can protect ourselves when developing a running system.

## Design Principles

1. We want all components in each runtime host (e.g., `ApiHost1`) to have access to static configuration (read-only), that would be set at deployment time. (i.e., in static files like `appsettings.json`)
2. We want that configuration to be over-written into static packaged assets (like `appsettings.json`) just before deployment, in the CD pipeline. Rather than as design time by a developer, so we avoid storing these settings in source code, or expose them to unprivileged people designing the system.
3. We want that configuration to be specific to a specific environment (e.g. `Local` development, `Staging`, or `Production`)
4. We do not want developers writing anything but `local` development environment settings (secrets or otherwise) into configuration files. With one exception, see note below.
5. We will need some "shared" configuration for the SaaS "platform" (used by all components), and some "private" configuration used by each "tenant" running on the platform. These two sets of configuration must be kept separate from each other, and may not be stored in the same repositories. (e.g. platform configuration is defined in `appsettings.json`, whilst tenancy configuration is stored in a data stores)
6. Configuration needs to be hierarchical (e.g. it can be grouped by namespace), and hierarchical in terms of layering.
7. Settings are expected to be of only 3 kinds: `string`, `number` and `boolean`
8. Components are responsible for reading their own configuration, and shall not re-use other components' configuration.
9. Secrets may be stored separately from non-confidential configuration in other repositories (e.g. files, databases, 3rd party services).
10. We want to be able to change storage location of configuration at any time, without breaking code (e.g. files, databases, 3rd party services).
11. We want to use dependency injection to give components their configuration.

> Point 4 above, with one exception: the configuration used to configure components for integration testing against real 3rd party systems (i.e. in tests of the category: `Integration.External`). These 3rd party accounts/environments, should never be related to production environments, and are designed only for testing-only. Configuration (and especially any secrets) used for these accounts/environments can NEVER lead those with access to them to compromise the system or its integrity.

## Implementation

The `IConfigurationSettings` abstraction is used to give access to configuration for both `Platform` settings and `Tenancy` settings.

It is injected into any adapters that require access to any configuration settings.

In order to operate effectively at runtime, the selection mechanism of whether to use a `Tenancy` setting or whether to use a `Platform` setting must be dynamic at runtime and must be dependent on whether the actual inbound HTTP request is destined for a specific tenant or not.

For these reasons, the `AspNetDynamicConfigurationSettings` adapter is to be used.

> This adapter is injected as both a "singleton" and a "scoped" in the DI container, depending on what adapters need which version of it.

### Platform Settings

Platform settings are "shared" across all components running in the platform, regardless of the lifetime of the dependency.

For example:

* Connection strings to centralized repositories (for hosting data pertaining to all tenants on the platform)
* Account details for accessing shared 3rd party system accounts via adapters (e.g., an email provider)
* Keys and defaults for various application and domain services

> Most of these settings will be stored in standard places that are supported by the .NET runtime, such as `appsettings.json` files for the specific environment.

### Tenancy Settings

Tenancy settings are "private" and are specific to a tenant running on the platform, they are only applicable to "scoped" dependencies.

For example:

* Connection strings to a tenant's physically partitioned repository (e.g., in a nearby data center of their choice)
* Account details for accessing a specific 3rd party system account via adapters (e.g., an accounting integration)

At runtime, in a multi-tenanted host, when the inbound HTTP request is destined for an API that is tenanted, the `ITenantContext` will define the tenancy, and it will define the `ITenancyContext.Settings` for the current HTTP request.

These settings are read from the `IOrganizationsRepository` (i.e., a data store), and can be updated by other APIs.

> These settings are never to be accidentally accessed by or exposed to other tenants running on the platform.