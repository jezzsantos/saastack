using System.Diagnostics.CodeAnalysis;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Hosting.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public const string PlatformKey = "PLATFORM";

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as a
    ///     to be resolved only by <see cref="ServiceProviderExtensions.GetRequiredServiceForPlatform{TService}" />
    /// </summary>
    public static IServiceCollection AddForPlatform<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        return services.AddKeyedSingleton<TService>(PlatformKey, (container, _) => implementationFactory(container));
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as a
    ///     to be resolved only by <see cref="ServiceProviderExtensions.GetRequiredServiceForPlatform{TService}" />
    /// </summary>
    public static IServiceCollection AddForPlatform<TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddKeyedSingleton<TService, TImplementation>(PlatformKey);
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    ///     to be resolved only by <see cref="ServiceProviderExtensions.GetRequiredServiceForPlatform{TService}" />
    /// </summary>
    public static IServiceCollection AddForPlatform<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
        where TService1 : class
        where TService2 : class
        where TService3 : class
        where TService4 : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        AddForPlatform(services, implementationFactory);
        services.AddKeyedSingleton<TService1>(PlatformKey,
            (svc, key) => svc.GetRequiredKeyedService<TImplementation>(key));
        services.AddKeyedSingleton<TService2>(PlatformKey,
            (svc, key) => svc.GetRequiredKeyedService<TImplementation>(key));
        services.AddKeyedSingleton<TService3>(PlatformKey,
            (svc, key) => svc.GetRequiredKeyedService<TImplementation>(key));
        services.AddKeyedSingleton<TService4>(PlatformKey,
            (svc, key) => svc.GetRequiredKeyedService<TImplementation>(key));

        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as per request (scoped),
    ///     only for services that must be initialized for each HTTP request
    /// </summary>
    public static IServiceCollection AddPerHttpRequest<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        return services.AddScoped(implementationFactory);
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as per request (scoped),
    ///     only for services that must be initialized for each HTTP request
    /// </summary>
    public static IServiceCollection AddPerHttpRequest<TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddScoped<TService, TImplementation>();
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as per request (scoped),
    ///     only for services that must be initialized for each HTTP request
    /// </summary>
    public static IServiceCollection AddPerHttpRequest(this IServiceCollection services,
        Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        return services.AddScoped(serviceType, implementationFactory);
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    ///     as per request (scoped), only for services that must be initialized for each HTTP request
    /// </summary>
    public static IServiceCollection AddPerHttpRequest<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        AddPerHttpRequest(services, implementationFactory);
        AddPerHttpRequest(services, typeof(TService1), svc => svc.GetRequiredService<TImplementation>());
        AddPerHttpRequest(services, typeof(TService2), svc => svc.GetRequiredService<TImplementation>());
        AddPerHttpRequest(services, typeof(TService3), svc => svc.GetRequiredService<TImplementation>());
        AddPerHttpRequest(services, typeof(TService4), svc => svc.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" /> and <see cref="TService2" />
    /// </summary>
    public static IServiceCollection AddSingleton<TService1, TService2, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TService1), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService2), svc => svc.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" /> and <see cref="TService3" />
    /// </summary>
    public static IServiceCollection AddSingleton<TService1, TService2, TService3, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TService1), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService2), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService3), svc => svc.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    /// </summary>
    public static IServiceCollection AddSingleton<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TService1), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService2), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService3), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService4), svc => svc.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" />, <see cref="TService4" /> and
    ///     <see cref="TService5" />
    /// </summary>
    public static IServiceCollection AddSingleton<TService1, TService2, TService3, TService4, TService5,
        TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4, TService5
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddSingleton(implementationFactory);
        services.AddSingleton(typeof(TService1), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService2), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService3), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService4), svc => svc.GetRequiredService<TImplementation>());
        services.AddSingleton(typeof(TService5), svc => svc.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> for the specified <see cref="lifetime" />
    /// </summary>
    public static IServiceCollection AddWithLifetime<TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TImplementation>(
        this IServiceCollection services, ServiceLifetime lifetime)
        where TService : class
        where TImplementation : class, TService
    {
        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime);
        services.Add(descriptor);
        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> for the specified <see cref="lifetime" />
    /// </summary>
    public static IServiceCollection AddWithLifetime<TService>(this IServiceCollection services, DependencyScope scope,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        var lifetime = scope.ToLifetime();
        var descriptor = new ServiceDescriptor(typeof(TService), implementationFactory, lifetime);
        services.Add(descriptor);
        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> for the specified <see cref="lifetime" />
    /// </summary>
    public static IServiceCollection AddWithLifetime(this IServiceCollection services, DependencyScope scope,
        Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        var lifetime = scope.ToLifetime();
        var descriptor = new ServiceDescriptor(serviceType, implementationFactory, lifetime);
        services.Add(descriptor);
        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    ///     for the specified <see cref="lifetime" />
    /// </summary>
    public static IServiceCollection AddWithLifetime<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, DependencyScope scope,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        AddWithLifetime(services, scope, implementationFactory);
        AddWithLifetime(services, scope, typeof(TService1), svc => svc.GetRequiredService<TImplementation>());
        AddWithLifetime(services, scope, typeof(TService2), svc => svc.GetRequiredService<TImplementation>());
        AddWithLifetime(services, scope, typeof(TService3), svc => svc.GetRequiredService<TImplementation>());
        AddWithLifetime(services, scope, typeof(TService4), svc => svc.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Whether the registration for the specified <see cref="TService" /> already exists or not
    /// </summary>
    public static bool IsRegistered<TService>(this IServiceCollection services)
    {
        return services.Any(svc => svc.ServiceType == typeof(TService));
    }

    /// <summary>
    ///     Whether the registration for the specified <see cref="TService" /> already exists or not
    /// </summary>
    public static bool IsRegisteredForPlatform<TService>(this IServiceCollection services)
        where TService : notnull
    {
        return services.Any(svc => svc.ServiceType == typeof(TService)
                                   && svc.IsKeyedService
                                   && svc.ServiceKey!.Equals(PlatformKey));
    }
}