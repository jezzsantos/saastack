using Amazon.Lambda.SQSEvents;
using Application.Persistence.Interfaces;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost.Extensions;

public static class LambdaExtensions
{
    /// <summary>
    ///     Handles the delivery of the message
    /// </summary>
    public static async Task<bool> HandleDelivery<TMessage>(this IMessageDeliveryHandler handler,
        SQSEvent receivedMessage, IMessageBusMonitoringApiRelayWorker<TMessage> worker, string subscriberHostName,
        string subscriptionName, CancellationToken cancellationToken)
        where TMessage : IQueuedMessage
    {
        const int deliveryCount = 0; //TODO: how to get these from Lambda runtime?
        const int retryCount = 5;
        try
        {
            await receivedMessage.RelayRecordsAsync(worker, subscriberHostName, subscriptionName, cancellationToken);
            await handler.CompleteMessageAsync(receivedMessage, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            await handler.AbandonMessageAsync(receivedMessage, cancellationToken);
            await handler.CheckCircuitAsync(handler.FunctionName, deliveryCount, retryCount, cancellationToken);
            throw;
        }
    }
}