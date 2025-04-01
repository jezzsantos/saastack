using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Azure.Messaging.ServiceBus;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class DeliverDomainEvents
{
    private readonly IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> _worker;

    public DeliverDomainEvents(
        IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverDomainEvents))]
    public async Task Run(
        [ServiceBusTrigger(WorkerConstants.MessageBuses.Topics.DomainEvents,
            "ApiHost1_EndUsersInfrastructure_Notifications_OrganizationNotificationConsumer",
            IsSessionsEnabled = true, AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message, ServiceBusMessageActions actions, FunctionContext context)
    {
        try
        {
            var msg = message.Body.ToObjectFromJson<DomainEventingMessage>();

            await _worker.RelayMessageOrThrowAsync("ApiHost1",
                "ApiHost1_OrganizationsInfrastructure_Notifications_EndUserNotificationConsumer", msg,
                context.CancellationToken);

            await actions.CompleteMessageAsync(message, context.CancellationToken);
        }
        catch (Exception)
        {
            await actions.AbandonMessageAsync(message, null, context.CancellationToken);

            //TODO: disable the function
            throw;
        }
    }
}