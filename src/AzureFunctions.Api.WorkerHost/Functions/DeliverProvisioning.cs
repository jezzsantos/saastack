using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class DeliverProvisioning
{
    private readonly IQueueMonitoringApiRelayWorker<ProvisioningMessage> _worker;

    public DeliverProvisioning(IQueueMonitoringApiRelayWorker<ProvisioningMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverProvisioning))]
    public Task Run([QueueTrigger(WorkerConstants.Queues.Provisionings)] ProvisioningMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}