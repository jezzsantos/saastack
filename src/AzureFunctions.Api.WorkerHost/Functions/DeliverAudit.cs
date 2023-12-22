using Application.Persistence.Shared;
using Infrastructure.Workers.Api;
using Infrastructure.Workers.Api.Workers;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

public sealed class DeliverAudit
{
    private readonly IQueueMonitoringApiRelayWorker<AuditMessage> _worker;

    public DeliverAudit(IQueueMonitoringApiRelayWorker<AuditMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverAudit))]
    public Task Run([QueueTrigger(DeliverAuditRelayWorker.QueueName)] AuditMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}