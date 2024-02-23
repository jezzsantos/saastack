namespace Domain.Interfaces.Services;

/// <summary>
///     Defined a dependency injection container
/// </summary>
public interface IDependencyContainer
{
    TService Resolve<TService>()
        where TService : notnull;

    TService ResolveForPlatform<TService>()
        where TService : notnull;
}