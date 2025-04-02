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
    private readonly IWorkersRuntime _runtime;
    private readonly IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> _worker;

    public DeliverDomainEvents(
        IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> worker, IWorkersRuntime runtime)
    {
        _worker = worker;
        _runtime = runtime;
    }

    [Function(nameof(DeliverDomainEvents))]
    public async Task Run(
        [ServiceBusTrigger(WorkerConstants.MessageBuses.Topics.DomainEvents,
            "ApiHost1_EndUsersInfrastructure_Notifications_OrganizationNotificationConsumer",
            IsSessionsEnabled = true, AutoCompleteMessages = false)]
        ServiceBusReceivedMessage receivedMessage, ServiceBusMessageActions actions, FunctionContext context)
    {
        var deliveryCount = receivedMessage.DeliveryCount;
        var retryCount =
            receivedMessage.ApplicationProperties.TryGetValue("x-opt-abort-retry-count", out var retryCountValue)
                ? (int)retryCountValue
                : 0;
        try
        {
            var message = receivedMessage.Body.ToObjectFromJson<DomainEventingMessage>();

            await _worker.RelayMessageOrThrowAsync("ApiHost1",
                "ApiHost1_OrganizationsInfrastructure_Notifications_EndUserNotificationConsumer", message,
                context.CancellationToken);

            await actions.CompleteMessageAsync(receivedMessage, context.CancellationToken);
        }
        catch (Exception)
        {
            await actions.AbandonMessageAsync(receivedMessage, null, context.CancellationToken);

            // https://dev.to/azure/serverless-circuit-breakers-with-durable-entities-3l2f
            if (retryCount == deliveryCount)
            {
                var workerName = context.FunctionDefinition.Name;
                await _runtime.CircuitBreakWorkerAsync(workerName, context.CancellationToken);
            }

            throw;
        }
    }
}