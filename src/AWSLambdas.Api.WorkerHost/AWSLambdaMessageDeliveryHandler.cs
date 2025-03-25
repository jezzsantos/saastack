using Amazon.Lambda.SQSEvents;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost;

public class AWSLambdaMessageDeliveryHandler : IMessageDeliveryHandler
{
    private readonly IWorkersRuntime _runtime;

    public AWSLambdaMessageDeliveryHandler(IWorkersRuntime runtime, string functionName, int retryCount)
    {
        _runtime = runtime;
        FunctionName = functionName;
        RetryCount = retryCount;
    }

    public Task AbandonMessageAsync(SQSEvent receivedMessage, CancellationToken cancellationToken)
    {
        //TODO: How to abandon message in AWS?
        return Task.CompletedTask;
    }

    public async Task CheckCircuitAsync(string workerName, int deliveryCount, int retryCount,
        CancellationToken cancellationToken)
    {
        if (ShouldOpenCircuit())
        {
            await _runtime.CircuitBreakWorkerAsync(workerName, cancellationToken);
        }

        return;

        bool ShouldOpenCircuit()
        {
            return deliveryCount >= retryCount - 1;
        }
    }

    public Task CompleteMessageAsync(SQSEvent receivedMessage, CancellationToken cancellationToken)
    {
        //TODO: How to complete message in AWS?
        return Task.CompletedTask;
    }

    public string FunctionName { get; }

    public int RetryCount { get; }
}