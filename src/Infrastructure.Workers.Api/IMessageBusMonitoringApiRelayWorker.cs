using Application.Persistence.Interfaces;

namespace Infrastructure.Workers.Api;

/// <summary>
///     Defines a message bus relay worker that relays messages to an API endpoint
/// </summary>
public interface IMessageBusMonitoringApiRelayWorker<in TQueuedMessage>
    where TQueuedMessage : IQueuedMessage
{
    /// <summary>
    ///     Relays the topic message
    /// </summary>
    Task RelayMessageOrThrowAsync(string subscriptionName, TQueuedMessage message, CancellationToken cancellationToken);
}