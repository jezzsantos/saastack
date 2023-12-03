using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines a source of notification events
/// </summary>
public interface IEventNotificationNotifier
{
    /// <summary>
    ///     Returns the producer/consumer registrations
    /// </summary>
    IReadOnlyList<IEventNotificationRegistration> Registrations { get; }

    /// <summary>
    ///     Writes the <see cref="events" /> from the stream
    /// </summary>
    Task<Result<Error>> WriteEventStreamAsync(string streamName, List<EventStreamChangeEvent> events,
        CancellationToken cancellationToken);
}