using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Interfaces.Clients;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Workers.Api.Workers;

public sealed class SendEmailRelayWorker : IQueueMonitoringApiRelayWorker<EmailMessage>
{
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;
    private readonly string _hmacSecret;

    public SendEmailRelayWorker(IRecorder recorder, IHostSettings settings,
        IServiceClientFactory serviceClientFactory) : this(recorder, serviceClientFactory,
        WorkerConstants.Queues.QueueDeliveryApiEndpoints[WorkerConstants.Queues.Emails](settings))
    {
    }

    private SendEmailRelayWorker(IRecorder recorder, IServiceClientFactory serviceClientFactory,
        (string BaseUrl, string HmacSecret) apiEndpointSettings) : this(recorder,
        serviceClientFactory.CreateServiceClient(apiEndpointSettings.BaseUrl), apiEndpointSettings.HmacSecret)
    {
    }

    private SendEmailRelayWorker(IRecorder recorder, IServiceClient serviceClient, string hmacSecret)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _hmacSecret = hmacSecret;
    }

    public async Task RelayMessageOrThrowAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        await _serviceClient.PostQueuedMessageToApiOrThrowAsync(_recorder,
            message, new SendEmailRequest
            {
                Message = message.ToJson()!
            }, _hmacSecret, cancellationToken);
    }
}