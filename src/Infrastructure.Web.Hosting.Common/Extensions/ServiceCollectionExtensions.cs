using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TInterface1" /> and <see cref="TInterface2" />
    /// </summary>
    public static IServiceCollection AddSingleton<TInterface1, TInterface2, TService1>(
        this IServiceCollection services, Func<IServiceProvider, TService1> implementationFactory)
        where TService1 : class, TInterface1, TInterface2
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TInterface1), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface2), c => c.GetRequiredService<TService1>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TInterface1" />, <see cref="TInterface2" /> and <see cref="TInterface3" />
    /// </summary>
    public static IServiceCollection AddSingleton<TInterface1, TInterface2, TInterface3, TService1>(
        this IServiceCollection services, Func<IServiceProvider, TService1> implementationFactory)
        where TService1 : class, TInterface1, TInterface2, TInterface3
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TInterface1), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface2), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface3), c => c.GetRequiredService<TService1>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TInterface1" />, <see cref="TInterface2" />, <see cref="TInterface3" /> and <see cref="TInterface4" />
    /// </summary>
    public static IServiceCollection AddSingleton<TInterface1, TInterface2, TInterface3, TInterface4, TService1>(
        this IServiceCollection services, Func<IServiceProvider, TService1> implementationFactory)
        where TService1 : class, TInterface1, TInterface2, TInterface3, TInterface4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TInterface1), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface2), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface3), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface4), c => c.GetRequiredService<TService1>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TInterface1" />, <see cref="TInterface2" />, <see cref="TInterface3" />, <see cref="TInterface4" /> and
    ///     <see cref="TInterface5" />
    /// </summary>
    public static IServiceCollection AddSingleton<TInterface1, TInterface2, TInterface3, TInterface4, TInterface5,
        TService1>(
        this IServiceCollection services, Func<IServiceProvider, TService1> implementationFactory)
        where TService1 : class, TInterface1, TInterface2, TInterface3, TInterface4, TInterface5
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TInterface1), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface2), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface3), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface4), c => c.GetRequiredService<TService1>());
        services.AddSingleton(typeof(TInterface5), c => c.GetRequiredService<TService1>());

        return services;
    }

    /// <summary>
    ///     Whether the registration for the specified <see cref="TInterface" /> already exists or not
    /// </summary>
    public static bool IsRegistered<TInterface>(this IServiceCollection services)
    {
        return services.Any(svc => svc.ServiceType == typeof(TInterface));
    }
}