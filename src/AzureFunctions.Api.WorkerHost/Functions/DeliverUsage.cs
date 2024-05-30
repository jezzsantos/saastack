using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class DeliverUsage
{
    private readonly IQueueMonitoringApiRelayWorker<UsageMessage> _worker;

    public DeliverUsage(IQueueMonitoringApiRelayWorker<UsageMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverUsage))]
    public Task Run([QueueTrigger(WorkerConstants.Queues.Usages)] UsageMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}