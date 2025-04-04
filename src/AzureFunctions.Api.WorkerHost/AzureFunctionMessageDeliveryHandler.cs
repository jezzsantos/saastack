using Azure.Messaging.ServiceBus;
using Infrastructure.Workers.Api;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost;

public class AzureFunctionMessageDeliveryHandler : IMessageDeliveryHandler
{
    private readonly ServiceBusMessageActions _actions;
    private readonly IWorkersRuntime _runtime;

    public AzureFunctionMessageDeliveryHandler(ServiceBusMessageActions actions, IWorkersRuntime runtime,
        string functionName, int retryCount)
    {
        _actions = actions;
        _runtime = runtime;
        FunctionName = functionName;
        RetryCount = retryCount;
    }

    public Task AbandonMessageAsync(ServiceBusReceivedMessage receivedMessage, CancellationToken cancellationToken)
    {
        return _actions.AbandonMessageAsync(receivedMessage, null, cancellationToken);
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

    public Task CompleteMessageAsync(ServiceBusReceivedMessage receivedMessage, CancellationToken cancellationToken)
    {
        return _actions.CompleteMessageAsync(receivedMessage, cancellationToken);
    }

    public string FunctionName { get; }

    public int RetryCount { get; }
}