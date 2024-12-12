using System.Collections.Concurrent;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Projections;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Hosting.Common.ApplicationServices.Eventing.Notifications;
using Infrastructure.Hosting.Common.ApplicationServices.Eventing.Projections;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Hosting.Common.Extensions;

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
    ///     <see cref="Application.Persistence.Interfaces.IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="Application.Persistence.Interfaces.ISnapshottingDddCommandStore{TAggregateRootOrEntity}" />, and
    ///     consumed by the specified <see cref="projectionFactory" /> and <see cref="notificationFactory" />
    /// </summary>
    public static IServiceCollection RegisterEventing<TAggregateRoot, TProjection, TNotificationRegistration>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        return AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(services, projectionFactory,
            notificationFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="Application.Persistence.Interfaces.IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="Application.Persistence.Interfaces.ISnapshottingDddCommandStore{TAggregateRootOrEntity}" />, and
    ///     consumed by the specified <see cref="projectionFactory" />
    /// </summary>
    public static IServiceCollection RegisterEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services, Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        return AddEventing<TAggregateRoot, TProjection>(services, projectionFactory);
    }

    /// <summary>
    ///     Configures event projection and event notification of events produced by the specified
    ///     <see cref="TAggregateRoot" /> and raised by both
    ///     <see cref="Application.Persistence.Interfaces.IEventSourcingDddCommandStore{TAggregateRoot}" /> and
    ///     <see cref="Application.Persistence.Interfaces.ISnapshottingDddCommandStore{TAggregateRootOrEntity}" />
    /// </summary>
    public static IServiceCollection RegisterEventing<TAggregateRoot>(
        this IServiceCollection services)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
    {
        return AddEventing<TAggregateRoot>(services);
    }

    private static IServiceCollection AddEventing<TAggregateRoot, TProjection, TNotificationRegistration>(
        this IServiceCollection services,
        Func<IServiceProvider, TProjection> projectionFactory,
        Func<IServiceProvider, TNotificationRegistration> notificationFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
        where TNotificationRegistration : class, IEventNotificationRegistration
    {
        AddEventing<TAggregateRoot, TProjection>(services, projectionFactory);
        Eventing.AddNotificationFactory<TAggregateRoot, TNotificationRegistration>();
        services.AddPerHttpRequest(notificationFactory);
        return services;
    }

    private static IServiceCollection AddEventing<TAggregateRoot, TProjection>(
        this IServiceCollection services,
        Func<IServiceProvider, TProjection> projectionFactory)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
        where TProjection : class, IReadModelProjection
    {
        AddEventing<TAggregateRoot>(services);
        Eventing.AddProjectionFactory<TAggregateRoot, TProjection>();
        services.AddPerHttpRequest(projectionFactory);
        return services;
    }

    private static IServiceCollection AddEventing<TAggregateRoot>(
        this IServiceCollection services)
        where TAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
    {
        if (!services.IsRegistered<IProjectionCheckpointRepository>())
        {
            services.AddPerHttpRequest<IProjectionCheckpointRepository>(c => new ProjectionCheckpointRepository(
                c.GetRequiredService<IRecorder>(), c.GetRequiredService<IIdentifierFactory>(),
                c.GetRequiredServiceForPlatform<IDataStore>()));
        }

        Eventing.AddEventingStorageTypes<TAggregateRoot>();
        if (!services.IsRegistered<IEventSourcingDddCommandStore<TAggregateRoot>>())
        {
            services.AddPerHttpRequest<IEventSourcingDddCommandStore<TAggregateRoot>>(c =>
                new EventSourcingDddCommandStore<TAggregateRoot>(c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcedChangeEventMigrator>(),
                    c.GetRequiredService<IEventStore>()));
        }

        if (!services.IsRegistered<ISnapshottingDddCommandStore<TAggregateRoot>>())
        {
            services.AddPerHttpRequest<ISnapshottingDddCommandStore<TAggregateRoot>>(c =>
                new SnapshottingDddCommandStore<TAggregateRoot>(c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IDataStore>()));
        }

        services.AddPerHttpRequest<IEventNotifyingStoreProjectionRelay>(c =>
            new InProcessEventNotifyingStoreProjectionRelay(
                c.GetRequiredService<IRecorder>(),
                c.GetRequiredService<IEventSourcedChangeEventMigrator>(),
                c.GetRequiredService<IProjectionCheckpointRepository>(),
                Eventing.ResolveProjections(c),
                Eventing.ResolveProjectionStores(c).ToArray()));
        services.AddPerHttpRequest<IEventNotifyingStoreNotificationRelay>(c =>
            new InProcessEventNotifyingStoreNotificationRelay(
                c.GetRequiredService<IRecorder>(),
                c.GetRequiredService<IEventSourcedChangeEventMigrator>(),
                c.GetRequiredService<IDomainEventConsumerRelay>(),
                c.GetRequiredService<IEventNotificationMessageBroker>(),
                Eventing.ResolveNotificationRegistrations(c),
                Eventing.ResolveNotificationStores(c).ToArray()));

        return services;
    }

    private sealed class EventingConfiguration
    {
        private readonly ConcurrentDictionary<Type, List<Type>> _eventingStorageTypes = new();
        private readonly ConcurrentDictionary<Type, List<Type>> _notificationFactories = new();
        private readonly ConcurrentDictionary<Type, List<Type>> _projectionFactories = new();

        public void AddEventingStorageTypes<TAggregateRoot>()
            where TAggregateRoot : IEventingAggregateRoot, IDehydratableEntity
        {
            var aggregateType = typeof(TAggregateRoot);
            _eventingStorageTypes.TryAdd(aggregateType, [
                typeof(IEventSourcingDddCommandStore<TAggregateRoot>),
                typeof(ISnapshottingDddCommandStore<TAggregateRoot>)
            ]);
        }

        public void AddNotificationFactory<TAggregateRoot, TNotificationRegistration>()
            where TAggregateRoot : IEventingAggregateRoot
            where TNotificationRegistration : IEventNotificationRegistration
        {
            var storageType = typeof(TAggregateRoot);
            _notificationFactories.TryAdd(storageType, new List<Type>());

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
            _projectionFactories.TryAdd(storageType, new List<Type>());

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