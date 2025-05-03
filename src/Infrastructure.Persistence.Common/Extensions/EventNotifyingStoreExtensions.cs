using Application.Persistence.Interfaces;
using AsyncKeyedLock;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Common.Extensions;

public static class EventNotifyingStoreExtensions
{
    private static readonly AsyncKeyedLocker<string> PublishingPipelineSection = new();

    /// <summary>
    ///     Saves and then publishes all events from the aggregate root to any event handlers.
    ///     Caution: This code is thread-safe and concurrency issues are mitigated, using semaphores for each aggregate.
    ///     However, critically it is not atomic, and can fail at any step the middle of this publishing pipeline,
    ///     either in publishing changes to the Message Bus, or in Projecting events to any read models. In either case,
    ///     if either of those steps fails, events will be lost (unreliable), and also the rest of the system that
    ///     subscribes to these events on the Message Bus will not get them, and be out of date,
    ///     AND/OR the projections for these events will be out of date.
    ///     A far more reliable pipeline would be to use "catch up subscriptions" on the actual event store.
    /// </summary>
    public static async Task<Result<Error>> SaveAndPublishChangesAsync<TAggregateRoot>(this IEventNotifyingStore store,
        TAggregateRoot aggregate, EventStreamChangedAsync<EventStreamChangedArgs>? eventHandler,
        Func<TAggregateRoot, List<EventSourcedChangeEvent>, CancellationToken, Task<Result<string, Error>>>
            onSaveEventStreamToStore, CancellationToken cancellationToken)
        where TAggregateRoot : IChangeEventProducingAggregateRoot, IIdentifiableEntity
    {
        var latestChanges = aggregate.GetChanges();
        if (latestChanges.IsFailure)
        {
            return latestChanges.Error;
        }

        var changedEvents = latestChanges.Value;
        if (changedEvents.HasNone())
        {
            return Result.Ok;
        }

        // Critical Section:
        // We want to lock, and to sequence, access to the same aggregate instance (by ID),
        // So that we don't have any race-conditions in any of the steps of the publishing pipeline below.
        // We only want to guard against updating two aggregates instances with the same ID (and same type),
        // definitely not to guard against access to any and all aggregate instances and types across the codebase
        // (since that would definitely slow down writing all aggregates events in this whole executable process,
        // and cause unacceptable performance issues).
        var lockKey = aggregate.Id.Value;
        using (await PublishingPipelineSection.LockAsync(lockKey, cancellationToken))
        {
            var saved = await onSaveEventStreamToStore(aggregate, changedEvents, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            aggregate.ClearChanges();

            var streamName = saved.Value;
            var published =
                await PublishAndProjectChangesAsync(store, eventHandler, changedEvents, streamName, cancellationToken);
            if (published.IsFailure)
            {
                return published.Error;
            }
        }

        return Result.Ok;
    }

    private static async Task<Result<Error>> PublishAndProjectChangesAsync(IEventNotifyingStore store,
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

    /// <summary>
    ///     Notifies the <see cref="store" /> of the changes.
    ///     Ultimately, the event that is raised on the store will trigger the event handlers already subscribed to the store.
    ///     This will trigger subscribers that both: Project (via the IReadModelProjector) AND Publish (via
    ///     IEventNotificationNotifier) this event
    /// </summary>
    private static async Task<Result<Error>> NotifyEventHandlers(IEventNotifyingStore store,
        EventStreamChangedAsync<EventStreamChangedArgs> eventHandler, string streamName,
        IReadOnlyList<EventStreamChangeEvent> changeEvents, CancellationToken cancellationToken)
    {
        var args = new EventStreamChangedArgs(changeEvents);
        eventHandler.Invoke(store, args, cancellationToken);

        Result<Error> completed;
        try
        {
            completed = await args.CompleteAsync();
            if (completed.IsSuccessful)
            {
                return Result.Ok;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                Resources.EventNotifyingStoreExtensions_OnComplete_FailedToNotify.Format(streamName),
                ex);
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