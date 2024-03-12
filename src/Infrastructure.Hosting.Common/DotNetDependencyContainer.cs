using Domain.Interfaces.Services;
using Infrastructure.Hosting.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides a simple dependency injection container that uses the .NET <see cref="IServiceProvider" />
/// </summary>
public class DotNetDependencyContainer : IDependencyContainer
{
    private readonly IServiceProvider _serviceProvider;

    public DotNetDependencyContainer(IServiceCollection serviceCollection)
    {
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public DotNetDependencyContainer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TService GetRequiredService<TService>()
        where TService : notnull
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    public TService GetRequiredServiceForPlatform<TService>()
        where TService : notnull
    {
        return _serviceProvider.GetRequiredServiceForPlatform<TService>();
    }
}