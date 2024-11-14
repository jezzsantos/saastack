using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class SendSms
{
    private readonly IQueueMonitoringApiRelayWorker<SmsMessage> _worker;

    public SendSms(IQueueMonitoringApiRelayWorker<SmsMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(SendSms))]
    public Task Run([QueueTrigger(WorkerConstants.Queues.Smses)] SmsMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}