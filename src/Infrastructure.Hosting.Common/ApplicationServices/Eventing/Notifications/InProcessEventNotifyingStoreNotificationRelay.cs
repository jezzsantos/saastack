using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Hosting.Common.ApplicationServices.Eventing.Notifications;

/// <summary>
///     Defines an in-process service that subscribes to one or more <see cref="IEventNotifyingStore" />
///     instances, listens to them raise change events, and relays them to a <see cref="IEventNotificationNotifier" />.
/// </summary>
public sealed class InProcessEventNotifyingStoreNotificationRelay : EventStreamHandlerBase,
    IEventNotifyingStoreNotificationRelay
{
    private readonly IEventNotificationNotifier _notifier;

    public InProcessEventNotifyingStoreNotificationRelay(IRecorder recorder, IEventSourcedChangeEventMigrator migrator,
        IDomainEventConsumerRelay consumerRelay,
        IEventNotificationMessageBroker messageBroker,
        IEnumerable<IEventNotificationRegistration> registrations,
        params IEventNotifyingStore[] eventingStores) : base(recorder, eventingStores)
    {
        _notifier = new EventNotificationNotifier(recorder, migrator, registrations.ToList(), consumerRelay,
            messageBroker);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            (_notifier as IDisposable)?.Dispose();
        }
    }

    protected override async Task<Result<Error>> HandleStreamEventsAsync(string streamName,
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken)
    {
        return await _notifier.WriteEventStreamAsync(streamName, eventStream, cancellationToken);
    }
}