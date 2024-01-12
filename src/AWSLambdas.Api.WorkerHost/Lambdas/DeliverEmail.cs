using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Application.Persistence.Shared.ReadModels;
using AWSLambdas.Api.WorkerHost.Extensions;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost.Lambdas;

public class DeliverEmail
{
    private readonly IQueueMonitoringApiRelayWorker<EmailMessage> _worker;

    public DeliverEmail(IQueueMonitoringApiRelayWorker<EmailMessage> worker)
    {
        _worker = worker;
    }

    [LambdaFunction]
    public async Task<bool> Run(SQSEvent sqsEvent, ILambdaContext context)
    {
        return await sqsEvent.RelayRecordsAsync(_worker, CancellationToken.None);
    }
}