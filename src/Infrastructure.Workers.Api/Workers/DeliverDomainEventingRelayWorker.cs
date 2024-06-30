using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using Infrastructure.Web.Interfaces.Clients;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Workers.Api.Workers;

public sealed class DeliverDomainEventingRelayWorker : IMessageBusMonitoringApiRelayWorker<DomainEventingMessage>
{
    private readonly IRecorder _recorder;
    private readonly IServiceClientFactory _serviceClientFactory;
    private readonly IReadOnlyList<SubscriberHost> _subscribers;

    public DeliverDomainEventingRelayWorker(IRecorder recorder, IHostSettings settings,
        IServiceClientFactory serviceClientFactory)
    {
        _recorder = recorder;
        _serviceClientFactory = serviceClientFactory;
        _subscribers = settings.GetEventNotificationSubscriberHosts();
    }

    public async Task RelayMessageOrThrowAsync(string subscriptionName, DomainEventingMessage message,
        CancellationToken cancellationToken)
    {
        var subscriber = _subscribers.FirstOrDefault(s => s.Id.EqualsIgnoreCase(subscriptionName));
        if (subscriber.NotExists())
        {
            throw new InvalidOperationException(
                Resources.DeliverDomainEventingRelayWorker_SubscriberNotFound.Format(subscriptionName));
        }

        var serviceClient = _serviceClientFactory.CreateServiceClient(subscriber.BaseUrl);

        await serviceClient.PostQueuedMessageToApiOrThrowAsync(_recorder,
            message, new NotifyDomainEventRequest
            {
                Message = message.ToJson()!
            }, subscriber.HmacSecret, cancellationToken);
    }
}