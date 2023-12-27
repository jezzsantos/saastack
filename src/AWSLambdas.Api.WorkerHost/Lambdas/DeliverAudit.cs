using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Application.Persistence.Shared;
using AWSLambdas.Api.WorkerHost.Extensions;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost.Lambdas;

public class DeliverAudit
{
    private readonly IQueueMonitoringApiRelayWorker<AuditMessage> _worker;

    public DeliverAudit(IQueueMonitoringApiRelayWorker<AuditMessage> worker)
    {
        _worker = worker;
    }

    [LambdaFunction]
    public async Task<bool> Run(SQSEvent sqsEvent, ILambdaContext context)
    {
        return await sqsEvent.RelayRecordsAsync(_worker, CancellationToken.None);
    }
}