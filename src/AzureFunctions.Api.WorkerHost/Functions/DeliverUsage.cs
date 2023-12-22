using Application.Persistence.Shared;
using Infrastructure.Workers.Api;
using Infrastructure.Workers.Api.Workers;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

public sealed class DeliverUsage
{
    private readonly IQueueMonitoringApiRelayWorker<UsageMessage> _worker;

    public DeliverUsage(IQueueMonitoringApiRelayWorker<UsageMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverUsage))]
    public Task Run([QueueTrigger(DeliverUsageRelayWorker.QueueName)] UsageMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}