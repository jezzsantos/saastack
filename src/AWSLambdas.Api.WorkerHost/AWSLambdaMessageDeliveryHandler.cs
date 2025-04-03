using Amazon.Lambda.SQSEvents;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost;

public class AWSLambdaMessageDeliveryHandler : IMessageDeliveryHandler
{
    private readonly IWorkersRuntime _runtime;

    public AWSLambdaMessageDeliveryHandler(IWorkersRuntime runtime, string functionName)
    {
        _runtime = runtime;
        FunctionName = functionName;
    }

    public Task AbandonMessageAsync(SQSEvent receivedMessage, CancellationToken cancellationToken)
    {
        //TODO: How to abandon message in AWS?
        return Task.CompletedTask;
    }

    public async Task CheckCircuitAsync(string workerName, int deliveryCount, int retryCount,
        CancellationToken cancellationToken)
    {
        if (deliveryCount >= retryCount)
        {
            await _runtime.CircuitBreakWorkerAsync(workerName, cancellationToken);
        }
    }

    public Task CompleteMessageAsync(SQSEvent receivedMessage, CancellationToken cancellationToken)
    {
        //TODO: How to complete message in AWS?
        return Task.CompletedTask;
    }

    public string FunctionName { get; }
}