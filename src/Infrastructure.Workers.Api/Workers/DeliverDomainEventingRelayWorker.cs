using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Workers.Api.Workers;

public sealed class DeliverDomainEventingRelayWorker : IMessageBusMonitoringApiRelayWorker<DomainEventingMessage>
{
    private readonly IRecorder _recorder;
    private readonly IServiceClientFactory _serviceClientFactory;
    private readonly IReadOnlyList<SubscriberHost> _subscriberHosts;

    public DeliverDomainEventingRelayWorker(IRecorder recorder, IHostSettings settings,
        IServiceClientFactory serviceClientFactory)
    {
        _recorder = recorder;
        _serviceClientFactory = serviceClientFactory;
        _subscriberHosts = settings.GetEventNotificationSubscriberHosts();
    }

    public async Task RelayMessageOrThrowAsync(string subscriberHostName, string subscriptionName,
        DomainEventingMessage message, CancellationToken cancellationToken)
    {
        var subscriberHost = _subscriberHosts.FirstOrDefault(s => s.HostName.EqualsIgnoreCase(subscriberHostName));
        if (subscriberHost.NotExists())
        {
            var ex = new InvalidOperationException(
                Resources.DeliverDomainEventingRelayWorker_SubscriberNotFound.Format(subscriberHostName));
            _recorder.TraceError(null, ex, "No registered subscriber host with name: {Host} found from configuration",
                subscriberHostName);
            throw ex;
        }

        var serviceClient = _serviceClientFactory.CreateServiceClient(subscriberHost.BaseUrl);
        await serviceClient.PostQueuedMessageToApiOrThrowAsync(_recorder,
            message, new NotifyDomainEventRequest
            {
                Message = message.ToJson()!,
                SubscriptionName = subscriptionName
            }, subscriberHost.HmacSecret, cancellationToken);
    }
}