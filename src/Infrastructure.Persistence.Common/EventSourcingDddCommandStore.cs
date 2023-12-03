using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Common.Events;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to individual <see cref="IEventSourcedAggregateRoot" /> DDD aggregate roots for [CQRS]
///     commands that use event sourced persistence
/// </summary>
public class EventSourcingDddCommandStore<TAggregateRoot> : IEventSourcingDddCommandStore<TAggregateRoot>
    where TAggregateRoot : IEventSourcedAggregateRoot
{
    private readonly IDomainFactory _domainFactory;
    private readonly string _entityName;
    private readonly IEventStore _eventStore;
    private readonly IEventSourcedChangeEventMigrator _migrator;
    private readonly IRecorder _recorder;

    public EventSourcingDddCommandStore(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcedChangeEventMigrator migrator, IEventStore eventStore)
    {
        _recorder = recorder;
        _eventStore = eventStore;
        _domainFactory = domainFactory;
        _migrator = migrator;
        _entityName = typeof(TAggregateRoot).GetEntityNameSafe();
    }

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

        var latestChanges = aggregate.GetChanges();
        if (!latestChanges.IsSuccessful)
        {
            return latestChanges.Error;
        }

        var changedEvents = latestChanges.Value;
        if (changedEvents.HasNone())
        {
            return Result.Ok;
        }

        var added = await _eventStore.AddEventsAsync(_entityName, aggregate.Id.Value, changedEvents, cancellationToken);
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        var streamName = added.Value;
        aggregate.ClearChanges();

        var raised = await PublishChangeEvents(changedEvents, streamName, cancellationToken);
        if (!raised.IsSuccessful)
        {
            return raised.Error;
        }

        return Result.Ok;
    }

    public event EventStreamChangedAsync<EventStreamChangedArgs>? OnEventStreamChanged;

    private static bool IsTombstoned(IEnumerable<EventSourcedChangeEvent> events)
    {
        var lastEvent = events.Last();
        var eventTypeName = lastEvent.Metadata;

        var tombstoneEventTypeName = typeof(Global.StreamDeleted).AssemblyQualifiedName;
        return eventTypeName == tombstoneEventTypeName;
    }

    private async Task<Result<Error>> PublishChangeEvents(IEnumerable<EventSourcedChangeEvent> changes,
        string streamName, CancellationToken cancellationToken)
    {
        if (OnEventStreamChanged.NotExists())
        {
            return Result.Ok;
        }

        var changeEvents = changes
            .Select(changeEvent => ToChangeEvent(changeEvent, streamName))
            .ToList();

        return await PublishToListeners(streamName, changeEvents, cancellationToken);
    }

    private async Task<Result<Error>> PublishToListeners(string streamName,
        IReadOnlyList<EventStreamChangeEvent> changeEvents,
        CancellationToken cancellationToken)
    {
        var args = new EventStreamChangedArgs(changeEvents);
        OnEventStreamChanged!.Invoke(this, args, cancellationToken);
        var completed = await args.CompleteAsync();
        return !completed.IsSuccessful
            ? completed.Error.Wrap(Resources.IEventSourcingDddCommandStore_PublishFailed.Format(streamName))
            : Result.Ok;
    }

    internal static EventStreamChangeEvent ToChangeEvent(EventSourcedChangeEvent changeEvent, string streamName)
    {
        return new EventStreamChangeEvent
        {
            Data = changeEvent.Data,
            EntityType = changeEvent.EntityType,
            EventType = changeEvent.EventType,
            Id = changeEvent.Id,
            LastPersistedAtUtc = changeEvent.LastPersistedAtUtc,
            Metadata = new EventMetadata(changeEvent.Metadata),
            StreamName = streamName,
            Version = changeEvent.Version
        };
    }

    private TAggregateRoot RehydrateAggregateRoot(ISingleValueObject<string> id, Optional<DateTime> lastPersistedAtUtc)
    {
        return (TAggregateRoot)_domainFactory.RehydrateAggregateRoot(typeof(TAggregateRoot),
            new HydrationProperties
            {
                { nameof(IEventSourcedAggregateRoot.Id), id.ToOptional<object>() },
                {
                    nameof(IEventSourcedAggregateRoot.LastPersistedAtUtc),
                    lastPersistedAtUtc.ValueOrDefault.ToOptional<object>()
                }
            });
    }
}