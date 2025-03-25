using Application.Persistence.Interfaces;
using Azure.Messaging.ServiceBus;
using Infrastructure.Workers.Api;

namespace AzureFunctions.Api.WorkerHost.Extensions;

public static class FunctionExtensions
{
    internal const string ServiceBusReceivedMessageDeliveryCountPropertyName = "x-opt-abort-retry-count";

    /// <summary>
    ///     Handles the delivery of the message
    /// </summary>
    public static async Task HandleDelivery<TMessage>(this IMessageDeliveryHandler handler,
        ServiceBusReceivedMessage receivedMessage, IMessageBusMonitoringApiRelayWorker<TMessage> worker,
        string subscriberHostName, string subscriptionName, CancellationToken cancellationToken)
        where TMessage : IQueuedMessage
    {
        var deliveryCount = receivedMessage.DeliveryCount;
        
        try
        {
            var message = receivedMessage.Body.ToObjectFromJson<TMessage>()!;
            await worker.RelayMessageOrThrowAsync(subscriberHostName, subscriptionName, message, cancellationToken);
            await handler.CompleteMessageAsync(receivedMessage, cancellationToken);
        }
        catch (Exception)
        {
            await handler.CheckCircuitAsync(handler.FunctionName, deliveryCount, cancellationToken);
            await handler.AbandonMessageAsync(receivedMessage, cancellationToken);
            throw;
        }
    }
}