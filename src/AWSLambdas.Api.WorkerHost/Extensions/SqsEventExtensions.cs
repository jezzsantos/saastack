using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using Application.Persistence.Interfaces;
using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost.Extensions;

public static class SqsEventExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public static async Task<bool> RelayRecordsAsync<TMessage>(this SQSEvent sqsEvent,
        IQueueMonitoringApiRelayWorker<TMessage> worker, CancellationToken cancellationToken)
        where TMessage : IQueuedMessage
    {
        var records = sqsEvent.Records;
        foreach (var body in records.Select(record => record.Body))
        {
            var message = JsonSerializer.Deserialize<TMessage>(body, JsonOptions)!;

            await worker.RelayMessageOrThrowAsync(message, cancellationToken);
        }

        return true;
    }

    public static async Task<bool> RelayRecordsAsync<TMessage>(this SQSEvent sqsEvent,
        IMessageBusMonitoringApiRelayWorker<TMessage> worker, string subscriptionName,
        CancellationToken cancellationToken)
        where TMessage : IQueuedMessage
    {
        var records = sqsEvent.Records;
        foreach (var body in records.Select(record => record.Body))
        {
            var message = JsonSerializer.Deserialize<TMessage>(body, JsonOptions)!;

            await worker.RelayMessageOrThrowAsync(subscriptionName, message, cancellationToken);
        }

        return true;
    }
}