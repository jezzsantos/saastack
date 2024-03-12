using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Hosting.Common.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    ///     Returns the registered instance of the registered <see cref="TService" /> from the container,
    ///     only for instances that were registered with the
    ///     <see cref="ServiceCollectionExtensions.AddForPlatform{TService}" />.
    ///     For example: to support multi-tenancy. Some services are registered twice in the container,
    ///     once specifically by tenanted instances, another specifically for untenanted/platform instances
    /// </summary>
    public static TService GetRequiredServiceForPlatform<TService>(this IServiceProvider services)
        where TService : notnull
    {
        return services.GetRequiredKeyedService<TService>(ServiceCollectionExtensions.PlatformKey);
    }

    /// <summary>
    ///     Returns a function that returns the registered instance of the registered <see cref="TService" /> from the
    ///     container, only for instances that were NOT registered with the
    ///     <see cref="ServiceCollectionExtensions.AddForPlatform{TService}" />.
    ///     Used to resolve services that are not shared (i.e. neither Platform/Tenanted)
    /// </summary>
    public static Func<TService> LazyGetRequiredService<TService>(this IServiceProvider services)
        where TService : notnull
    {
        // ReSharper disable once ConvertClosureToMethodGroup
        return () => services.GetRequiredService<TService>();
    }
}