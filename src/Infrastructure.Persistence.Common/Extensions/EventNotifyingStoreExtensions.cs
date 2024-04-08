using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Common.Extensions;

public static class EventNotifyingStoreExtensions
{
    /// <summary>
    ///     Saves and then publishes all events from the aggregate root to any event handlers
    /// </summary>
    public static async Task<Result<Error>> SaveAndPublishChangesAsync<TAggregateRoot>(this IEventNotifyingStore store,
        TAggregateRoot aggregate, EventStreamChangedAsync<EventStreamChangedArgs>? eventHandler,
        Func<TAggregateRoot, List<EventSourcedChangeEvent>, CancellationToken, Task<Result<string, Error>>> onSave,
        CancellationToken cancellationToken)
        where TAggregateRoot : IChangeEventProducingAggregateRoot
    {
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

        var saved = await onSave(aggregate, changedEvents, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        var streamName = saved.Value;

        aggregate.ClearChanges();

        var raised = await PublishChangesAsync(store, eventHandler, changedEvents, streamName, cancellationToken);
        if (!raised.IsSuccessful)
        {
            return raised.Error;
        }

        return Result.Ok;
    }

    private static async Task<Result<Error>> PublishChangesAsync(IEventNotifyingStore store,
        EventStreamChangedAsync<EventStreamChangedArgs>? eventHandler,
        IEnumerable<EventSourcedChangeEvent> changes, string streamName, CancellationToken cancellationToken)
    {
        if (eventHandler.NotExists())
        {
            return Result.Ok;
        }

        var changeEvents = changes
            .Select(changeEvent => ToChangeEvent(changeEvent, streamName))
            .ToList();

        return await NotifyEventHandlers(store, eventHandler, streamName, changeEvents, cancellationToken);
    }

    private static EventStreamChangeEvent ToChangeEvent(EventSourcedChangeEvent changeEvent, string streamName)
    {
        return new EventStreamChangeEvent
        {
            Data = changeEvent.Data,
            RootAggregateType = changeEvent.EntityType,
            EventType = changeEvent.EventType,
            Id = changeEvent.Id,
            LastPersistedAtUtc = changeEvent.LastPersistedAtUtc,
            Metadata = new EventMetadata(changeEvent.Metadata),
            StreamName = streamName,
            Version = changeEvent.Version
        };
    }

    private static async Task<Result<Error>> NotifyEventHandlers(IEventNotifyingStore store,
        EventStreamChangedAsync<EventStreamChangedArgs> eventHandler, string streamName,
        IReadOnlyList<EventStreamChangeEvent> changeEvents, CancellationToken cancellationToken)
    {
        var args = new EventStreamChangedArgs(changeEvents);
        eventHandler.Invoke(store, args, cancellationToken);
        var completed = await args.CompleteAsync();

        if (completed.IsSuccessful)
        {
            return Result.Ok;
        }

        var error = completed.Error;
        if (error.Code == ErrorCode.Unexpected)
        {
            return error;
        }

        return completed.Error.Wrap(ErrorCode.Unexpected,
            Resources.EventSourcingDddCommandStore_PublishFailed.Format(streamName));
    }
}