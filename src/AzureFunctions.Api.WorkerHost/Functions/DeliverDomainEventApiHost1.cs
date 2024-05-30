using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class DeliverDomainEventApiHost1
{
    private readonly IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> _worker;

    public DeliverDomainEventApiHost1(IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverDomainEventApiHost1))]
    public Task Run(
        [ServiceBusTrigger(WorkerConstants.MessageBuses.Topics.DomainEvents,
            WorkerConstants.MessageBuses.Subscribers.ApiHost1)]
        DomainEventingMessage message, FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(WorkerConstants.MessageBuses.Subscribers.ApiHost1, message,
            context.CancellationToken);
    }
}