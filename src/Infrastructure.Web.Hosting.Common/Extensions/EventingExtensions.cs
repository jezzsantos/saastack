using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Projections;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices.Eventing.Notifications;
using Infrastructure.Web.Hosting.Common.ApplicationServices.Eventing.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class EventingExtensions
{
    private static readonly EventingConfiguration Eventing = new();

    /// <summary>
    ///     Clears all eventing configuration
    /// </summary>
    internal static void ClearEventConfiguration()
    {
        Eventing.Reset();
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="ISnapshottingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" /> and <see cref="notificationFactory" />
    /// </summary>
    public static IServiceCollection RegisterPlatformEventing<TAggregateRoot, TProjection,
        TNotificationRegistration>(this IServiceCollection services,
        Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        return services.AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(DependencyScope.Platform,
            projectionFactory, notificationFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="ISnapshottingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" />
    /// </summary>
    public static IServiceCollection RegisterPlatformEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        return services.AddEventing<TAggregateRoot, TProjection>(DependencyScope.Platform,
            projectionFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="ISnapshottingDddCommandStore{TAggregateRoot}" />
    /// </summary>
    public static IServiceCollection RegisterPlatformEventing<TAggregateRoot>(
        this IServiceCollection services)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
    {
        return services.AddEventing<TAggregateRoot>(DependencyScope.Platform);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="ISnapshottingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" /> and <see cref="notificationFactory" />
    /// </summary>
    public static IServiceCollection RegisterTenantedEventing<TAggregateRoot, TProjection, TNotificationRegistration>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        return services.AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(DependencyScope.PerTenant,
            projectionFactory, notificationFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="ISnapshottingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" />
    /// </summary>
    public static IServiceCollection RegisterTenantedEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        return services.AddEventing<TAggregateRoot, TProjection>(DependencyScope.PerTenant, projectionFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="ISnapshottingDddCommandStore{TAggregateRoot}" />
    /// </summary>
    public static IServiceCollection RegisterTenantedEventing<TAggregateRoot>(
        this IServiceCollection services)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
    {
        return services.AddEventing<TAggregateRoot>(DependencyScope.PerTenant);
    }

    private static IServiceCollection AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(
        this IServiceCollection services, DependencyScope scope,
        Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        services.AddEventing<TAggregateRoot, TProjection>(DependencyScope.PerTenant, projectionFactory);
        Eventing.AddNotificationFactory<TAggregateRoot, TNotificationRegistration>();
        services.RegisterLifetime(scope, notificationFactory);
        return services;
    }

    private static IServiceCollection AddEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services, DependencyScope scope,
        Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        services.AddEventing<TAggregateRoot>(DependencyScope.PerTenant);
        Eventing.AddProjectionFactory<TAggregateRoot, TProjection>();
        services.RegisterLifetime(scope, projectionFactory);
        return services;
    }

    private static IServiceCollection AddEventing<TAggregateRoot>(
        this IServiceCollection services, DependencyScope scope)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
    {
        if (!services.IsRegistered<IProjectionCheckpointRepository>())
        {
            services.RegisterUnshared<IProjectionCheckpointRepository>(c => new ProjectionCheckpointRepository(
                c.ResolveForUnshared<IRecorder>(), c.ResolveForUnshared<IIdentifierFactory>(),
                c.ResolveForUnshared<IDomainFactory>(), c.ResolveForPlatform<IDataStore>()));
        }

        Eventing.AddEventingStorageTypes<TAggregateRoot>();
        if (!services.IsRegistered<IEventSourcingDddCommandStore<TAggregateRoot>>())
        {
            IEventStore EventStoreFactory(IServiceProvider c)
            {
                return scope switch
                {
                    DependencyScope.PerTenant => c.ResolveForTenant<IEventStore>(),
                    _ => c.ResolveForPlatform<IEventStore>()
                };
            }

            services.RegisterLifetime<IEventSourcingDddCommandStore<TAggregateRoot>>(scope,
                c => new EventSourcingDddCommandStore<TAggregateRoot>(c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(), c.ResolveForUnshared<IEventSourcedChangeEventMigrator>(),
                    EventStoreFactory(c)));
        }

        if (!services.IsRegistered<ISnapshottingDddCommandStore<TAggregateRoot>>())
        {
            IDataStore DataStoreFactory(IServiceProvider c)
            {
                return scope switch
                {
                    DependencyScope.PerTenant => c.ResolveForTenant<IDataStore>(),
                    _ => c.ResolveForPlatform<IDataStore>()
                };
            }

            services.RegisterLifetime<ISnapshottingDddCommandStore<TAggregateRoot>>(scope,
                c => new SnapshottingDddCommandStore<TAggregateRoot>(c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(), DataStoreFactory(c)));
        }

        services.RegisterTenanted<IEventNotifyingStoreProjectionRelay>(c =>
            new InProcessSynchronousProjectionRelay(
                c.ResolveForUnshared<IRecorder>(),
                c.ResolveForUnshared<IEventSourcedChangeEventMigrator>(),
                c.ResolveForUnshared<IProjectionCheckpointRepository>(),
                Eventing.ResolveProjections(c),
                Eventing.ResolveProjectionStores(c).ToArray()));
        services.RegisterTenanted<IEventNotifyingStoreNotificationRelay>(c =>
            new InProcessSynchronousNotificationRelay(
                c.ResolveForUnshared<IRecorder>(),
                c.ResolveForUnshared<IEventSourcedChangeEventMigrator>(),
                Eventing.ResolveNotificationRegistrations(c),
                Eventing.ResolveNotificationStores(c).ToArray()));

        return services;
    }

    private sealed class EventingConfiguration
    {
        private readonly Dictionary<Type, List<Type>> _eventingStorageTypes = new();
        private readonly Dictionary<Type, List<Type>> _notificationFactories = new();
        private readonly Dictionary<Type, List<Type>> _projectionFactories = new();

        public void AddEventingStorageTypes<TAggregateRoot>()
            where TAggregateRoot : IEventingAggregateRoot, IDehydratableEntity
        {
            var aggregateType = typeof(TAggregateRoot);
            _eventingStorageTypes.TryAdd(aggregateType, new List<Type>
            {
                typeof(IEventSourcingDddCommandStore<TAggregateRoot>),
                typeof(ISnapshottingDddCommandStore<TAggregateRoot>)
            });
        }

        public void AddNotificationFactory<TAggregateRoot, TNotificationRegistration>()
            where TAggregateRoot : IEventingAggregateRoot
            where TNotificationRegistration : IEventNotificationRegistration
        {
            var storageType = typeof(TAggregateRoot);
            if (!_notificationFactories.ContainsKey(storageType))
            {
                _notificationFactories.Add(storageType,
                    new List<Type>());
            }

            var pubSubPairType = typeof(TNotificationRegistration);
            var aggregateFactories = _notificationFactories[storageType];
            if (!aggregateFactories.Contains(pubSubPairType))
            {
                aggregateFactories.Add(pubSubPairType);
            }
        }

        public void AddProjectionFactory<TAggregateRoot, TReadModelProjection>()
            where TAggregateRoot : IEventingAggregateRoot
            where TReadModelProjection : IReadModelProjection
        {
            var storageType = typeof(TAggregateRoot);
            if (!_projectionFactories.ContainsKey(storageType))
            {
                _projectionFactories.Add(storageType,
                    new List<Type>());
            }

            var projectionType = typeof(TReadModelProjection);
            var aggregateFactories = _projectionFactories[storageType];
            if (!aggregateFactories.Contains(projectionType))
            {
                aggregateFactories.Add(projectionType);
            }
        }

        public void Reset()
        {
            _eventingStorageTypes.Clear();
            _projectionFactories.Clear();
            _notificationFactories.Clear();
        }

        public IEnumerable<IEventNotificationRegistration> ResolveNotificationRegistrations(IServiceProvider container)
        {
            var registrations = _notificationFactories
                .SelectMany(pair =>
                    pair.Value.Select(type =>
                        container.GetRequiredService(type) as IEventNotificationRegistration))
                .ToList();
            return registrations!;
        }

        public List<IEventNotifyingStore> ResolveNotificationStores(IServiceProvider container)
        {
            var storages = _notificationFactories
                .Select(pair => _eventingStorageTypes[pair.Key])
                .SelectMany(stores =>
                {
                    return stores.Select(store =>
                        container.GetRequiredService(store) as IEventNotifyingStore);
                })
                .ToList();
            return storages!;
        }

        public IEnumerable<IReadModelProjection> ResolveProjections(IServiceProvider container)
        {
            var projections = _projectionFactories
                .SelectMany(
                    pair => pair.Value.Select(type => container.GetRequiredService(type) as IReadModelProjection))
                .ToList();
            return projections!;
        }

        public List<IEventNotifyingStore> ResolveProjectionStores(IServiceProvider container)
        {
            var storages = _projectionFactories
                .Select(pair => _eventingStorageTypes[pair.Key])
                .SelectMany(stores =>
                {
                    return stores.Select(store =>
                        container.GetRequiredService(store) as IEventNotifyingStore);
                })
                .ToList();
            return storages!;
        }
    }
}