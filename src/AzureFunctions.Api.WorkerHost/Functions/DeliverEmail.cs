using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

public sealed class DeliverEmail
{
    private readonly IQueueMonitoringApiRelayWorker<EmailMessage> _worker;

    public DeliverEmail(IQueueMonitoringApiRelayWorker<EmailMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverEmail))]
    public Task Run([QueueTrigger(WorkerConstants.Queues.Emails)] EmailMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}