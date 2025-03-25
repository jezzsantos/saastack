using Azure.Messaging.ServiceBus;
using Common.Configuration;
using Infrastructure.Workers.Api;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost;

public class AzureFunctionMessageDeliveryHandler : IMessageDeliveryHandler
{
    internal const int DefaultMaxDeliveryCountBeforeFailure = 5;
    internal const string MaxDeliveryCountBeforeFailureSettingName =
        "Hosts:AzureFunctions:MaxDeliveryCountBeforeFailure";
    private readonly ServiceBusMessageActions _actions;
    private readonly int _maxDeliveryCount;
    private readonly IWorkersRuntime _runtime;

    public AzureFunctionMessageDeliveryHandler(IConfigurationSettings settings, ServiceBusMessageActions actions,
        IWorkersRuntime runtime, string functionName)
    {
        _actions = actions;
        _runtime = runtime;
        FunctionName = functionName;
        _maxDeliveryCount = (int)
            settings.Platform.GetNumber(MaxDeliveryCountBeforeFailureSettingName, DefaultMaxDeliveryCountBeforeFailure);
    }

    public Task AbandonMessageAsync(ServiceBusReceivedMessage receivedMessage, CancellationToken cancellationToken)
    {
        return _actions.AbandonMessageAsync(receivedMessage, null, cancellationToken);
    }

    public async Task CheckCircuitAsync(string workerName, int deliveryCount, CancellationToken cancellationToken)
    {
        if (ShouldOpenCircuit())
        {
            await _runtime.CircuitBreakWorkerAsync(workerName, cancellationToken);
        }

        return;

        bool ShouldOpenCircuit()
        {
            return deliveryCount >= _maxDeliveryCount - 1;
        }
    }

    public Task CompleteMessageAsync(ServiceBusReceivedMessage receivedMessage, CancellationToken cancellationToken)
    {
        return _actions.CompleteMessageAsync(receivedMessage, cancellationToken);
    }

    public string FunctionName { get; }
}