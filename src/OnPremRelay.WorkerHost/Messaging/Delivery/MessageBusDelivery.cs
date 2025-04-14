using System.Text.Json;
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
    public Task ProcessMessageAsync(TMessage message, string subscriptionName,
        CancellationToken cancellationToken)
    {
        return _worker.RelayMessageOrThrowAsync(SubscriberHostName, subscriptionName, message, cancellationToken);
    }

    public async Task HandleDeliveryAsync<TMessage>(
        string json,
        string routingKey,
        Func<TMessage, CancellationToken, Task> processMessage,
        int failureCount,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        try
        {
            TMessage message = JsonSerializer.Deserialize<TMessage>(json);
            if (message == null)
            {
                throw new InvalidOperationException("El mensaje deserializado resultó nulo.");
            }

            await processMessage(message, cancellationToken);
            await this.CompleteMessageAsync(json, cancellationToken);
        }
        catch (Exception)
        {
            await this.CheckCircuitAsync(this.SubscriberHostName, failureCount, cancellationToken);
            await this.AbandonMessageAsync(json, cancellationToken);
            throw; // relanzamos para que se pueda aplicar la lógica de reintento o notificar el fallo
        }
    }

    public Task CompleteMessageAsync(object messageContext, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AbandonMessageAsync(object messageContext, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task CheckCircuitAsync(string workerName, int currentFailureCount, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}