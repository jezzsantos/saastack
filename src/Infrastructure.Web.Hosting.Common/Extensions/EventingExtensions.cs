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
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" /> and <see cref="notificationFactory" />
    /// </summary>
    public static IServiceCollection AddPlatformPersistenceStoreEventing<TAggregateRoot, TProjection,
        TNotificationRegistration>(this IServiceCollection services,
        Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventSourcedAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        return services.AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(DependencyScope.Platform,
            projectionFactory, notificationFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" />
    /// </summary>
    public static IServiceCollection AddPlatformPersistenceStoreEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventSourcedAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        return services.AddEventing<TAggregateRoot, TProjection>(DependencyScope.Platform,
            projectionFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" /> and <see cref="notificationFactory" />
    /// </summary>
    public static IServiceCollection AddTenantedEventing<TAggregateRoot, TProjection, TNotificationRegistration>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventSourcedAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        return services.AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(DependencyScope.PerTenant,
            projectionFactory, notificationFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="IEventSourcingDddCommandStore{TAggregateRoot}" />, and consumed by the specified
    ///     <see cref="projectionFactory" />
    /// </summary>
    public static IServiceCollection AddTenantedEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventSourcedAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        return services.AddEventing<TAggregateRoot, TProjection>(DependencyScope.PerTenant, projectionFactory);
    }

    /// <summary>
    ///     Clears all eventing configuration
    /// </summary>
    internal static void ClearEventConfiguration()
    {
        Eventing.Reset();
    }

    private static IServiceCollection AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(
        this IServiceCollection services, DependencyScope scope,
        Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventSourcedAggregateRoot
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
        where TAggregateRoot : IEventSourcedAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        if (!services.IsRegistered<IReadModelCheckpointRepository>())
        {
            services.RegisterUnshared<IReadModelCheckpointRepository>(c => new ReadModelCheckpointRepository(
                c.ResolveForUnshared<IRecorder>(), c.ResolveForUnshared<IIdentifierFactory>(),
                c.ResolveForUnshared<IDomainFactory>(), c.ResolveForPlatform<IDataStore>()));
        }

        Eventing.AddEventingStorageType<TAggregateRoot>();
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

        Eventing.AddProjectionFactory<TAggregateRoot, TProjection>();
        services.RegisterLifetime(scope, projectionFactory);

        services.RegisterTenanted<IEventNotifyingStoreProjectionRelay>(c =>
            new InProcessNotifyingStoreProjectionRelay(
                c.ResolveForUnshared<IRecorder>(),
                c.ResolveForUnshared<IEventSourcedChangeEventMigrator>(),
                c.ResolveForUnshared<IReadModelCheckpointRepository>(),
                Eventing.ResolveProjections(c),
                Eventing.ResolveProjectionStores(c).ToArray()));
        services.RegisterTenanted<IEventNotifyingStoreNotificationRelay>(c =>
            new InProcessEventNotifyingStoreNotificationRelay(
                c.ResolveForUnshared<IRecorder>(),
                c.ResolveForUnshared<IEventSourcedChangeEventMigrator>(),
                Eventing.ResolveNotificationRegistrations(c),
                Eventing.ResolveNotificationStores(c).ToArray()));

        return services;
    }

    private sealed class EventingConfiguration
    {
        private readonly Dictionary<Type, Type> _eventingStorageTypes = new();
        private readonly Dictionary<Type, List<Type>> _notificationFactories = new();
        private readonly Dictionary<Type, List<Type>> _projectionFactories = new();

        public void AddEventingStorageType<TAggregateRoot>()
            where TAggregateRoot : IEventSourcedAggregateRoot
        {
            var aggregateType = typeof(TAggregateRoot);
            var eventingStorageType = typeof(IEventSourcingDddCommandStore<TAggregateRoot>);
            _eventingStorageTypes.TryAdd(aggregateType, eventingStorageType);
        }

        public void AddNotificationFactory<TAggregateRoot, TNotificationRegistration>()
            where TAggregateRoot : IEventSourcedAggregateRoot
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
            where TAggregateRoot : IEventSourcedAggregateRoot
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
                .Select(pair => container.GetRequiredService(_eventingStorageTypes[pair.Key]) as IEventNotifyingStore)
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
                .Select(pair => container.GetRequiredService(_eventingStorageTypes[pair.Key]) as IEventNotifyingStore)
                .ToList();
            return storages!;
        }
    }
}