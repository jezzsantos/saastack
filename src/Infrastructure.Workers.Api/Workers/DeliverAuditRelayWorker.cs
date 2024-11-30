using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Interfaces.Clients;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Workers.Api.Workers;

public sealed class DeliverAuditRelayWorker : IQueueMonitoringApiRelayWorker<AuditMessage>
{
    private readonly string _hmacSecret;
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public DeliverAuditRelayWorker(IRecorder recorder, IHostSettings settings,
        IServiceClientFactory serviceClientFactory) : this(recorder, serviceClientFactory,
        WorkerConstants.Queues.QueueDeliveryApiEndpoints[WorkerConstants.Queues.Audits](settings))
    {
    }

    private DeliverAuditRelayWorker(IRecorder recorder, IServiceClientFactory serviceClientFactory,
        (string BaseUrl, string HmacSecret) apiEndpointSettings) : this(recorder,
        serviceClientFactory.CreateServiceClient(apiEndpointSettings.BaseUrl), apiEndpointSettings.HmacSecret)
    {
    }

    private DeliverAuditRelayWorker(IRecorder recorder, IServiceClient serviceClient, string hmacSecret)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _hmacSecret = hmacSecret;
    }

    public async Task RelayMessageOrThrowAsync(AuditMessage message, CancellationToken cancellationToken)
    {
        await _serviceClient.PostQueuedMessageToApiOrThrowAsync(_recorder,
            message, new DeliverAuditRequest
            {
                Message = message.ToJson()!
            }, _hmacSecret, cancellationToken);
    }
}