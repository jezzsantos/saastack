namespace Domain.Interfaces.Services;

/// <summary>
///     Defines a dependency injection container
/// </summary>
public interface IDependencyContainer
{
    TService GetRequiredService<TService>()
        where TService : notnull;

    TService GetRequiredServiceForPlatform<TService>()
        where TService : notnull;
}