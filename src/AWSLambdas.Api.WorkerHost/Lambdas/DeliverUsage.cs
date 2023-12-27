using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Application.Persistence.Shared;
using AWSLambdas.Api.WorkerHost.Extensions;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost.Lambdas;

public class DeliverUsage
{
    private readonly IQueueMonitoringApiRelayWorker<UsageMessage> _worker;

    public DeliverUsage(IQueueMonitoringApiRelayWorker<UsageMessage> worker)
    {
        _worker = worker;
    }

    [LambdaFunction]
    public async Task<bool> Run(SQSEvent sqsEvent, ILambdaContext context)
    {
        return await sqsEvent.RelayRecordsAsync(_worker, CancellationToken.None);
    }
}