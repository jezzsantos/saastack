using Amazon.Lambda.SQSEvents;

namespace AWSLambdas.Api.WorkerHost;

/// <summary>
///     Defines a message handler
/// </summary>
public interface IMessageDeliveryHandler
{
    string FunctionName { get; }

    int RetryCount { get; }

    Task AbandonMessageAsync(SQSEvent receivedMessage, CancellationToken cancellationToken);

    Task CheckCircuitAsync(string workerName, int deliveryCount, int retryCount, CancellationToken cancellationToken);

    Task CompleteMessageAsync(SQSEvent receivedMessage, CancellationToken cancellationToken);
}