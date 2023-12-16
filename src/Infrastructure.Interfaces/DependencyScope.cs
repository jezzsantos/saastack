namespace Infrastructure.Interfaces;

/// <summary>
///     Defines the scope of dependencies
/// </summary>
public enum DependencyScope
{
    UnTenanted =
        0, //Shared services that would have an instance in the container for untenanted use

    Tenanted =
        1 //Shared services that would have an instance in the container exclusively for each tenants use, and another for untenanted use
}