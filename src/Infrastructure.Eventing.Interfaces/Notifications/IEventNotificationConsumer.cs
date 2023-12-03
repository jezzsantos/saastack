using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines a consumer of events
/// </summary>
public interface IEventNotificationConsumer
{
    /// <summary>
    ///     Handles the notification of the <see cref="changeEvent" />, and returns whether it was handled or not
    /// </summary>
    Task<Result<bool, Error>> NotifyAsync(IDomainEvent changeEvent, CancellationToken cancellationToken);
}