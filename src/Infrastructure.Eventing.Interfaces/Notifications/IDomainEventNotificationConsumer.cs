using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines a consumer of domain events
/// </summary>
public interface IDomainEventNotificationConsumer
{
    /// <summary>
    ///     Handles the notification of a <see cref="IDomainEvent" />
    /// </summary>
    Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}