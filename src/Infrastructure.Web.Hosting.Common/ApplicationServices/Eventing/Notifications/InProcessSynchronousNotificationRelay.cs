using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Web.Hosting.Common.ApplicationServices.Eventing.Notifications;

/// <summary>
///     Defines an in-process service that subscribes to one or more <see cref="IEventNotifyingStore" />
///     instances, listens to them raise change events, and relays them to listening consumers synchronously.
/// </summary>
public class InProcessSynchronousNotificationRelay : EventStreamHandlerBase,
    IEventNotifyingStoreNotificationRelay
{
    public InProcessSynchronousNotificationRelay(IRecorder recorder, IEventSourcedChangeEventMigrator migrator,
        IEnumerable<IEventNotificationRegistration> registrations,
        params IEventNotifyingStore[] eventingStores) : base(recorder, eventingStores)
    {
        Notifier = new EventNotificationNotifier(recorder, migrator, registrations.ToArray());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            (Notifier as IDisposable)?.Dispose();
        }
    }

    public IEventNotificationNotifier Notifier { get; }

    protected override async Task<Result<Error>> HandleStreamEventsAsync(string streamName,
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken)
    {
        return await Notifier.WriteEventStreamAsync(streamName, eventStream, cancellationToken);
    }
}