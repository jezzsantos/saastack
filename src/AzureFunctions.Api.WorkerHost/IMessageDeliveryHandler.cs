using Azure.Messaging.ServiceBus;

namespace AzureFunctions.Api.WorkerHost;

/// <summary>
///     Defines a message handler
/// </summary>
public interface IMessageDeliveryHandler
{
    string FunctionName { get; }

    Task AbandonMessageAsync(ServiceBusReceivedMessage receivedMessage, CancellationToken cancellationToken);

    Task CheckCircuitAsync(string workerName, int deliveryCount, int retryCount, CancellationToken cancellationToken);

    Task CompleteMessageAsync(ServiceBusReceivedMessage receivedMessage, CancellationToken cancellationToken);
}