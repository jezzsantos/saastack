using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    ///     Returns the registered instance of the registered <see cref="TService" /> from the container,
    ///     only for instances that were NOT registered with the
    ///     <see cref="ServiceCollectionExtensions.RegisterPlatform{TService}" />.
    ///     Used to resolve services that are not shared (i.e. neither Platform/Tenanted)
    /// </summary>
    public static TService Resolve<TService>(this IServiceProvider container)
        where TService : notnull
    {
        return container.GetRequiredService<TService>();
    }

    /// <summary>
    ///     Returns the registered instance of the registered <see cref="TService" /> from the container,
    ///     only for instances that were registered with the
    ///     <see cref="ServiceCollectionExtensions.RegisterPlatform{TService}" />.
    ///     For example: to support multi-tenancy. Some services are registered twice in the container,
    ///     once specifically by tenanted instances, another specifically for untenanted/platform instances
    /// </summary>
    public static TService ResolveForPlatform<TService>(this IServiceProvider container)
        where TService : notnull
    {
        var dependency = container.GetRequiredService<IPlatformDependency<TService>>();
        return dependency.UnWrap();
    }

    /// <summary>
    ///     Returns the registered instance of the registered <see cref="TService" /> from the container,
    ///     only for instances that were NOT registered with the
    ///     <see cref="ServiceCollectionExtensions.RegisterPlatform{TService}" />.
    ///     For example: to support multi-tenancy. Some services are registered twice in the container,
    ///     once specifically by tenanted instances, another specifically for untenanted/platform instances
    /// </summary>
    public static TService ResolveForTenant<TService>(this IServiceProvider container)
        where TService : notnull
    {
        return container.GetRequiredService<TService>();
    }

    /// <summary>
    ///     Returns the registered instance of the registered <see cref="TService" /> from the container,
    ///     only for instances that were NOT registered with the
    ///     <see cref="ServiceCollectionExtensions.RegisterPlatform{TService}" />.
    ///     Used to resolve services that are not shared (i.e. neither Platform/Tenanted)
    /// </summary>
    public static TService ResolveForUnshared<TService>(this IServiceProvider container)
        where TService : notnull
    {
        return container.Resolve<TService>();
    }
}