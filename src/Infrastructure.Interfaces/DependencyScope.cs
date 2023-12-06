namespace Infrastructure.Interfaces;

/// <summary>
///     Defines the scope of dependencies
/// </summary>
public enum DependencyScope
{
    Platform =
        0, //Shared services that would have an instance in the container exclusively for the platform, and another for tenants

    PerTenant =
        1, //Shared services that would have an instance in the container exclusively for a tenant, and another for the platform
}