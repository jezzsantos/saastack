using System.Text.Json;
using Application.Persistence.Interfaces;
using Infrastructure.Workers.Api;

namespace OnPremRelay.WorkerHost.Messaging.Delivery;

/// <summary>
///     Provides a generic delivery service for queue messages.
/// </summary>
/// <typeparam name="TMessage">
///     The type of queued message. Must implement <see cref="IQueuedMessage" />.
/// </typeparam>
public class QueueDelivery<TMessage>
    where TMessage : IQueuedMessage
{
    private readonly IQueueMonitoringApiRelayWorker<TMessage> _worker;

    /// <summary>
    ///     Gets the name of the queue from which messages are delivered.
    /// </summary>
    public string QueueName { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="QueueDelivery{TMessage}" /> class.
    /// </summary>
    /// <param name="worker">The relay worker for the queued message.</param>
    /// <param name="queueName">The name of the queue.</param>
    public QueueDelivery(IQueueMonitoringApiRelayWorker<TMessage> worker, string queueName)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
    }

    /// <summary>
    ///     Processes the queued message asynchronously.
    /// </summary>
    /// <param name="message">The queued message.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ProcessMessageAsync(TMessage message, CancellationToken cancellationToken)
    {
        return _worker.RelayMessageOrThrowAsync(message, cancellationToken);
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
            await this.CheckCircuitAsync(this.QueueName, failureCount, cancellationToken);
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