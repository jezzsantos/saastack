using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Workers.Api.Workers;

public sealed class DeliverUsageRelayWorker : IQueueMonitoringApiRelayWorker<UsageMessage>
{
    private readonly string _hmacSecret;
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public DeliverUsageRelayWorker(IRecorder recorder, IHostSettings settings,
        IServiceClientFactory serviceClientFactory) : this(recorder, serviceClientFactory,
        WorkerConstants.Queues.QueueDeliveryApiEndpoints[WorkerConstants.Queues.Usages](settings))
    {
    }

    private DeliverUsageRelayWorker(IRecorder recorder, IServiceClientFactory serviceClientFactory,
        (string BaseUrl, string HmacSecret) apiEndpointSettings) : this(recorder,
        serviceClientFactory.CreateServiceClient(apiEndpointSettings.BaseUrl), apiEndpointSettings.HmacSecret)
    {
    }

    private DeliverUsageRelayWorker(IRecorder recorder, IServiceClient serviceClient, string hmacSecret)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _hmacSecret = hmacSecret;
    }

    public async Task RelayMessageOrThrowAsync(UsageMessage message, CancellationToken cancellationToken)
    {
        await _serviceClient.PostQueuedMessageToApiOrThrowAsync(_recorder,
            message, new DeliverUsageRequest
            {
                Message = message.ToJson()!
            }, _hmacSecret, cancellationToken);
    }
}