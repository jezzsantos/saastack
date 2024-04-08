using System.Reflection;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Common.Events;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to individual <see cref="IEventingAggregateRoot" /> DDD aggregate roots for [CQRS]
///     commands that use event sourced persistence
/// </summary>
public class EventSourcingDddCommandStore<TAggregateRoot> : IEventSourcingDddCommandStore<TAggregateRoot>
    where TAggregateRoot : IEventingAggregateRoot
{
    private readonly IDomainFactory _domainFactory;
    private readonly string _entityName;
    private readonly IEventStore _eventStore;
    private readonly IEventSourcedChangeEventMigrator _migrator;
    private readonly IRecorder _recorder;

    public EventSourcingDddCommandStore(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcedChangeEventMigrator migrator, IEventStore eventStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _eventStore = eventStore;
        _domainFactory = domainFactory;
        _migrator = migrator;
        _entityName = GetEntityName();
    }

    public Guid InstanceId { get; }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _eventStore.DestroyAllAsync(_entityName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All events were deleted for the event stream {Entity} in the {Store} store",
                _entityName, _eventStore.GetType().Name);
        }

        return deleted;
    }

    public async Task<Result<TAggregateRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        return await _recorder.MeasureWithDuration<Task<Result<TAggregateRoot, Error>>>(null,
            "EventingDddCommandPersistence.Load", async context =>
            {
                var eventStream = await _eventStore.GetEventStreamAsync(_entityName, id, cancellationToken);
                if (!eventStream.IsSuccessful)
                {
                    return eventStream.Error;
                }

                var events = eventStream.Value;
                if (events.HasNone())
                {
                    return Error.EntityNotFound();
                }

                AddMeasurementData(context, events);

                if (IsTombstoned(events))
                {
                    return Error.EntityNotFound(Resources.IEventSourcingDddCommandStore_StreamTombstoned);
                }

                var lastPersistedAtUtc = events.Last().LastPersistedAtUtc;
                var aggregate = RehydrateAggregateRoot(id, lastPersistedAtUtc);
                aggregate.LoadChanges(events, _migrator);

                return aggregate;
            });

        void AddMeasurementData(IDictionary<string, object> context,
            IReadOnlyCollection<EventSourcedChangeEvent> events)
        {
            context.Add("EntityName", _entityName);
            context.Add("EntityId", id);
            context.Add("EventCount", events.Count.ToString());
        }
    }

    public async Task<Result<Error>> SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken)
    {
        if (aggregate.Id.IsEmpty())
        {
            return Error.EntityExists(Resources.IEventSourcingDddCommandStore_SaveWithAggregateIdMissing);
        }

        var published = await this.SaveAndPublishChangesAsync(aggregate, OnEventStreamChanged,
            (root, changedEvents, token) =>
                _eventStore.AddEventsAsync(_entityName, root.Id.Value, changedEvents, token), cancellationToken);
        if (!published.IsSuccessful)
        {
            return published.Error;
        }

        return Result.Ok;
    }

    public event EventStreamChangedAsync<EventStreamChangedArgs>? OnEventStreamChanged;

    private static string GetEntityName()
    {
        var customAttribute = typeof(TAggregateRoot).GetCustomAttribute<EntityNameAttribute>();
        if (customAttribute.Exists())
        {
            return customAttribute.EntityName;
        }

        var name = typeof(TAggregateRoot).Name;
        if (name.EndsWith("Entity"))
        {
            name = name.Substring(0, name.LastIndexOf("Entity", StringComparison.Ordinal));
        }

        if (name.EndsWith("Aggregate"))
        {
            name = name.Substring(0, name.LastIndexOf("Aggregate", StringComparison.Ordinal));
        }

        if (name.EndsWith("Root"))
        {
            name = name.Substring(0, name.LastIndexOf("Root", StringComparison.Ordinal));
        }

        return name;
    }

    private static bool IsTombstoned(IEnumerable<EventSourcedChangeEvent> events)
    {
        var lastEvent = events.Last();
        var eventTypeName = lastEvent.Metadata;

        var tombstoneEventTypeName = typeof(Global.StreamDeleted).AssemblyQualifiedName;
        return eventTypeName == tombstoneEventTypeName;
    }

    private TAggregateRoot RehydrateAggregateRoot(ISingleValueObject<string> id, Optional<DateTime> lastPersistedAtUtc)
    {
        return (TAggregateRoot)_domainFactory.RehydrateAggregateRoot(typeof(TAggregateRoot),
            new HydrationProperties
            {
                { nameof(IEventingAggregateRoot.Id), id.ToOptional<object>() },
                {
                    nameof(IEventingAggregateRoot.LastPersistedAtUtc),
                    lastPersistedAtUtc.ValueOrDefault.ToOptional<object>()
                }
            });
    }
}