using Common;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines a message broker for receiving and publishing  integration events
/// </summary>
public interface IEventNotificationMessageBroker
{
    /// <summary>
    ///     Publishes the <see cref="IIntegrationEvent" /> to some message broker
    /// </summary>
    Task<Result<Error>> PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}