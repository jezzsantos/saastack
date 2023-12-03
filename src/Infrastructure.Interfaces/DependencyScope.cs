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
    NotSharedSingleton = 2, //Services that are not shared, where there is one instance in the container
    NotSharedScoped = 3 //Services that are not shared, where there is one instance in the container
}