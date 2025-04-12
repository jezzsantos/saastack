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

    /// <summary>
    ///     Gets the name of the queue from which messages are delivered.
    /// </summary>
    public string SubscriberHostName { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageBusDelivery{TMessage}" /> class.
    /// </summary>
    /// <param name="worker">The message bus relay worker.</param>
    /// <param name="subscriber">The subscriber name.</param>
    public MessageBusDelivery(IMessageBusMonitoringApiRelayWorker<TMessage> worker,
        string subscriberHostName)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        SubscriberHostName = subscriberHostName ?? throw new ArgumentNullException(nameof(subscriberHostName));
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
    public Task ProcessMessageAsync(TMessage message, string subscriptionName,
        CancellationToken cancellationToken)
    {
        return _worker.RelayMessageOrThrowAsync(SubscriberHostName, subscriptionName, message, cancellationToken);
    }
}