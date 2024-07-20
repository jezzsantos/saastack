using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class SendEmail
{
    private readonly IQueueMonitoringApiRelayWorker<EmailMessage> _worker;

    public SendEmail(IQueueMonitoringApiRelayWorker<EmailMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(SendEmail))]
    public Task Run([QueueTrigger(WorkerConstants.Queues.Emails)] EmailMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}