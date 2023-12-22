using Application.Persistence.Interfaces;

namespace Infrastructure.Workers.Api;

/// <summary>
///     Defines a queue trigger worker that relays messages to an API endpoint
/// </summary>
public interface IQueueMonitoringApiRelayWorker<in TQueuedMessage>
    where TQueuedMessage : IQueuedMessage
{
    /// <summary>
    ///     Relays the queued message
    /// </summary>
    Task RelayMessageOrThrowAsync(TQueuedMessage message, CancellationToken cancellationToken);
}