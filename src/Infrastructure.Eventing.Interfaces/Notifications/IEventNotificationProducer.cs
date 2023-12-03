using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines a producer of events from a domain aggregate root
/// </summary>
public interface IEventNotificationProducer
{
    /// <summary>
    ///     Returns the type of the root aggregate that produces the events
    /// </summary>
    Type RootAggregateType { get; }

    /// <summary>
    ///     Handles the notification of a new <see cref="changeEvent" />, and returns the actual event to publish
    ///     to downstream consumers
    /// </summary>
    Task<Result<Optional<IDomainEvent>, Error>> PublishAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken);
}