using System.Diagnostics.CodeAnalysis;
using Infrastructure.Common;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Hosting.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Whether the registration for the specified <see cref="TService" /> already exists or not
    /// </summary>
    public static bool IsPlatformRegistered<TService>(this IServiceCollection services)
        where TService : notnull
    {
        return services.Any(svc => svc.ServiceType == typeof(IPlatformDependency<TService>));
    }

    /// <summary>
    ///     Whether the registration for the specified <see cref="TService" /> already exists or not
    /// </summary>
    public static bool IsRegistered<TService>(this IServiceCollection services)
    {
        return services.Any(svc => svc.ServiceType == typeof(TService));
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> for the specified <see cref="lifetime" />
    /// </summary>
    public static IServiceCollection RegisterLifetime<TService,
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
    public static IServiceCollection RegisterLifetime<TService>(this IServiceCollection services, DependencyScope scope,
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
    public static IServiceCollection RegisterLifetime(this IServiceCollection services, DependencyScope scope,
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
    public static IServiceCollection RegisterLifetime<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, DependencyScope scope,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterLifetime(services, scope, implementationFactory);
        RegisterLifetime(services, scope, typeof(TService1), c => c.Resolve<TImplementation>());
        RegisterLifetime(services, scope, typeof(TService2), c => c.Resolve<TImplementation>());
        RegisterLifetime(services, scope, typeof(TService3), c => c.Resolve<TImplementation>());
        RegisterLifetime(services, scope, typeof(TService4), c => c.Resolve<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as a
    ///     <see cref="Infrastructure.Interfaces.IPlatformDependency{TService}" />
    ///     to be resolved only by <see cref="ServiceProviderExtensions.ResolveForPlatform{TService}" />
    /// </summary>
    public static IServiceCollection RegisterPlatform<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        services.AddSingleton(typeof(IPlatformDependency<TService>),
            container => new PlatformDependency<TService>(implementationFactory(container)));
        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    /// </summary>
    public static IServiceCollection RegisterPlatform<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterPlatform(services, implementationFactory);
        services.AddSingleton(typeof(IPlatformDependency<TService1>),
            c => c.Resolve<IPlatformDependency<TImplementation>>());
        services.AddSingleton(typeof(IPlatformDependency<TService2>),
            c => c.Resolve<IPlatformDependency<TImplementation>>());
        services.AddSingleton(typeof(IPlatformDependency<TService3>),
            c => c.Resolve<IPlatformDependency<TImplementation>>());
        services.AddSingleton(typeof(IPlatformDependency<TService4>),
            c => c.Resolve<IPlatformDependency<TImplementation>>());

        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as per request (scoped),
    ///     only for services that must be initialized for tenanted services
    /// </summary>
    public static IServiceCollection RegisterTenanted<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        services.AddScoped(implementationFactory);
        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as per request (scoped),
    ///     only for services that must be initialized for tenanted services
    /// </summary>
    public static IServiceCollection RegisterTenanted<TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddScoped<TService, TImplementation>();
        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as per request (scoped),
    ///     only for services that must be initialized for tenanted services
    /// </summary>
    public static IServiceCollection RegisterTenanted(this IServiceCollection services,
        Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        services.AddScoped(serviceType, implementationFactory);
        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    ///     as per request (scoped), only for services that must be initialized for tenanted services
    /// </summary>
    public static IServiceCollection RegisterTenanted<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterTenanted(services, implementationFactory);
        RegisterTenanted(services, typeof(TService1), c => c.ResolveForTenant<TImplementation>());
        RegisterTenanted(services, typeof(TService2), c => c.ResolveForTenant<TImplementation>());
        RegisterTenanted(services, typeof(TService3), c => c.ResolveForTenant<TImplementation>());
        RegisterTenanted(services, typeof(TService4), c => c.ResolveForTenant<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as a singleton.
    ///     Only for services that are neither tenanted nor platform
    /// </summary>
    public static IServiceCollection RegisterUnshared<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        services.AddSingleton(implementationFactory);
        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as a singleton.
    ///     Only for services that are neither tenanted nor platform
    /// </summary>
    public static IServiceCollection RegisterUnshared<TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddSingleton<TService, TImplementation>();
        return services;
    }

    /// <summary>
    ///     Registers an instance of the <see cref="TService" /> as a singleton.
    ///     Only for services that are neither tenanted nor platform
    /// </summary>
    public static IServiceCollection RegisterUnshared(this IServiceCollection services,
        Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        services.AddSingleton(serviceType, implementationFactory);
        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" /> and <see cref="TService2" />
    /// </summary>
    public static IServiceCollection RegisterUnshared<TService1, TService2, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterUnshared(services, implementationFactory);
        RegisterUnshared(services, typeof(TService1), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService2), c => c.ResolveForUnshared<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" /> and <see cref="TService3" />
    /// </summary>
    public static IServiceCollection RegisterUnshared<TService1, TService2, TService3, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterUnshared(services, implementationFactory);
        RegisterUnshared(services, typeof(TService1), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService2), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService3), c => c.ResolveForUnshared<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" /> and <see cref="TService4" />
    /// </summary>
    public static IServiceCollection RegisterUnshared<TService1, TService2, TService3, TService4, TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterUnshared(services, implementationFactory);
        RegisterUnshared(services, typeof(TService1), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService2), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService3), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService4), c => c.ResolveForUnshared<TImplementation>());

        return services;
    }

    /// <summary>
    ///     Registers the <see cref="implementationFactory" /> for the specified interfaces:
    ///     <see cref="TService1" />, <see cref="TService2" />, <see cref="TService3" />, <see cref="TService4" /> and
    ///     <see cref="TService5" />
    /// </summary>
    public static IServiceCollection RegisterUnshared<TService1, TService2, TService3, TService4, TService5,
        TImplementation>(
        this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, TService1, TService2, TService3, TService4, TService5
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RegisterUnshared(services, implementationFactory);
        RegisterUnshared(services, typeof(TService1), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService2), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService3), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService4), c => c.ResolveForUnshared<TImplementation>());
        RegisterUnshared(services, typeof(TService5), c => c.ResolveForUnshared<TImplementation>());

        return services;
    }
}