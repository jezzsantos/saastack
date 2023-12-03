using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a notifier of change events from registered producer to registered consumers
/// </summary>
public sealed class EventNotificationNotifier : IEventNotificationNotifier, IDisposable
{
    private readonly IEventSourcedChangeEventMigrator _migrator;
    private readonly IRecorder _recorder;

    public EventNotificationNotifier(IRecorder recorder, IEventSourcedChangeEventMigrator migrator,
        params IEventNotificationRegistration[] registrations)
    {
        _recorder = recorder;
        Registrations = registrations;
        _migrator = migrator;
    }

    ~EventNotificationNotifier()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (Registrations.Any())
        {
            foreach (var pair in Registrations)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                (pair.Producer as IDisposable)?.Dispose();
                // ReSharper disable once SuspiciousTypeConversion.Global
                (pair.Consumer as IDisposable)?.Dispose();
            }
        }
    }

    public IReadOnlyList<IEventNotificationRegistration> Registrations { get; }

    public async Task<Result<Error>> WriteEventStreamAsync(string streamName, List<EventStreamChangeEvent> eventStream,
        CancellationToken cancellationToken)
    {
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        if (!eventStream.Any())
        {
            return Result.Ok;
        }

        if (!Registrations.Any())
        {
            return Result.Ok;
        }

        var streamEntityType = Enumerable.First(eventStream).EntityType;
        var registration = GetProducerForStream(Registrations, streamEntityType);
        if (!registration.HasValue)
        {
            return Result.Ok;
        }

        foreach (var changeEvent in eventStream)
        {
            var deserialized = DeserializeEvent(changeEvent, _migrator);
            if (!deserialized.IsSuccessful)
            {
                return deserialized.Error;
            }

            var relayed = await RelayEventAsync(registration.Value, deserialized.Value, changeEvent,
                cancellationToken);
            if (!relayed.IsSuccessful)
            {
                return relayed.Error;
            }
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> RelayEventAsync(IEventNotificationRegistration registration,
        IDomainEvent @event, EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var published = await registration.Producer.PublishAsync(@event, cancellationToken);
        if (!published.IsSuccessful)
        {
            return published.Error.Wrap(Resources.EventNotificationNotifier_ProducerError.Format(
                registration.Producer.GetType().Name,
                changeEvent.Id, changeEvent.Metadata.Fqn));
        }

        var publishedEvent = published.Value;
        if (!publishedEvent.HasValue)
        {
            _recorder.TraceInformation(null,
                "The producer '{Producer}' chose not publish the event '{Event}' with event type '{Type}'",
                registration.Producer.GetType().Name,
                changeEvent.Id, changeEvent.Metadata.Fqn);
            return Result.Ok;
        }

        var notified = await registration.Consumer.NotifyAsync(publishedEvent.Value, cancellationToken);
        if (!notified.IsSuccessful)
        {
            return notified.Error;
        }

        if (!notified.Value)
        {
            return Error.RuleViolation(
                Resources.EventNotificationNotifier_ConsumerError.Format(registration.Consumer.GetType().Name,
                    changeEvent.Id, changeEvent.Metadata.Fqn));
        }

        return Result.Ok;
    }

    private static Optional<IEventNotificationRegistration> GetProducerForStream(
        IEnumerable<IEventNotificationRegistration> registrations, string entityTypeName)
    {
        return new Optional<IEventNotificationRegistration>(
            registrations.FirstOrDefault(prj => prj.Producer.RootAggregateType.Name == entityTypeName));
    }

    private static Result<IDomainEvent, Error> DeserializeEvent(EventStreamChangeEvent changeEvent,
        IEventSourcedChangeEventMigrator migrator)
    {
        return changeEvent.Metadata.CreateEventFromJson(changeEvent.Id, changeEvent.Data, migrator);
    }
}