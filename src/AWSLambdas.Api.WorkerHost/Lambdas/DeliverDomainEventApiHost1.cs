using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using AWSLambdas.Api.WorkerHost.Extensions;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost.Lambdas;

public class DeliverDomainEventApiHost1
{
    private readonly IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> _worker;

    public DeliverDomainEventApiHost1(IMessageBusMonitoringApiRelayWorker<DomainEventingMessage> worker)
    {
        _worker = worker;
    }

    [LambdaFunction]
    public async Task<bool> Run(SQSEvent sqsEvent, ILambdaContext context)
    {
        return await sqsEvent.RelayRecordsAsync(_worker, WorkerConstants.MessageBuses.Subscribers.ApiHost1,
            CancellationToken.None);
    }
}