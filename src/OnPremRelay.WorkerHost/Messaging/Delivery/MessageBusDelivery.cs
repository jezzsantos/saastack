using Application.Persistence.Interfaces;
using Infrastructure.Workers.Api;

namespace OnPremRelay.WorkerHost.Messaging.Delivery;

/// <summary>
///     Provides a generic delivery service for message bus messages.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message. Must implement <see cref="IQueuedMessage" />.
/// </typeparam>
public class MessageBusDelivery<TMessage>
    where TMessage : IQueuedMessage
{
    private readonly IMessageBusMonitoringApiRelayWorker<TMessage> _worker;
    private readonly string _subscriberHostName;
    private readonly string _subscriptionName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageBusDelivery{TMessage}" /> class.
    /// </summary>
    /// <param name="worker">The message bus relay worker.</param>
    /// <param name="subscriber">The subscriber name.</param>
    public MessageBusDelivery(IMessageBusMonitoringApiRelayWorker<TMessage> worker, string subscriberHostName,
        string subscriptionName)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _subscriberHostName = subscriberHostName ?? throw new ArgumentNullException(nameof(subscriberHostName));
        _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
    }

    /// <summary>
    ///     Processes the message bus message asynchronously.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ProcessMessageAsync(TMessage message, CancellationToken cancellationToken)
    {
        return _worker.RelayMessageOrThrowAsync(_subscriberHostName, _subscriptionName, message, cancellationToken);
    }
}