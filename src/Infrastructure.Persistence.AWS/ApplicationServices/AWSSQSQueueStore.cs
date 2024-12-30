using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Persistence.AWS.Extensions;
using Infrastructure.Persistence.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.AWS.ApplicationServices;

/// <summary>
///     Provides a queue store for AWS Simple Queue Service (SQS) Queues.
///     Note: ContentDeDuplication is turned ON, as message JSON are expected to be unique (by MessageId) even for messages
///     with the same properties
///     Note: This store uses FIFO queues, with dead letter queues as backups
/// </summary>
public class AWSSQSQueueStore : IQueueStore
{
    private readonly Dictionary<string, string> _knownQueueUrls;
    private readonly IRecorder _recorder;
    private readonly AmazonSQSClient _serviceClient;

    public static AWSSQSQueueStore Create(IRecorder recorder, IConfigurationSettings settings)
    {
        var (credentials, regionEndpoint) = settings.GetConnection();
        if (regionEndpoint.Exists())
        {
            var remoteClient = new AmazonSQSClient(credentials, regionEndpoint);
            return new AWSSQSQueueStore(recorder, remoteClient);
        }
        
        var localStackClient = new AmazonSQSClient(credentials,
            new AmazonSQSConfig
            {
                ServiceURL = AWSConstants.LocalStackServiceUrl,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName
            });

        return new AWSSQSQueueStore(recorder, localStackClient);
    }

#if TESTINGONLY
    public static AWSSQSQueueStore Create(IRecorder recorder, string localStackServiceUrl)
    {
        var localStackClient = new AmazonSQSClient(new AnonymousAWSCredentials(),
            new AmazonSQSConfig
            {
                ServiceURL = localStackServiceUrl,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName
            });

        return new AWSSQSQueueStore(recorder, localStackClient);
    }
#endif

    private AWSSQSQueueStore(IRecorder recorder, AmazonSQSClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _knownQueueUrls = new Dictionary<string, string>();
    }
#if TESTINGONLY

    public async Task<Result<long, Error>> CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AWSSQSQueueStore_MissingQueueName);

        var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
        if (!queueUrl.HasValue)
        {
            _recorder.TraceInformation(null, "Queue not found: {Queue}", queueName);
            return 0;
        }

        try
        {
            var response = await _serviceClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl.Value,
                AttributeNames = new List<string>
                {
                    nameof(GetQueueAttributesResponse.ApproximateNumberOfMessages)
                }
            }, cancellationToken);

            return response.ApproximateNumberOfMessages;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to retrieve attributes from queue: {Queue}", queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }
#endif

    public async Task<Result<Error>> DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AWSSQSQueueStore_MissingQueueName);

        var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
        if (!queueUrl.HasValue)
        {
            return Result.Ok;
        }

        try
        {
            await _serviceClient.DeleteQueueAsync(queueUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to delete queue: {Queue}", queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }

        _knownQueueUrls.Remove(queueName);
#else
        await Task.CompletedTask;
#endif

        return Result.Ok;
    }

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AWSSQSQueueStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
        if (!queueUrl.HasValue)
        {
            _recorder.TraceInformation(null, "Queue not found: {Queue}", queueUrl);
            return false;
        }

        var retrieved = await GetNextMessageAsync(queueUrl, cancellationToken);
        if (retrieved.IsFailure || !retrieved.Value.HasValue)
        {
            return false;
        }

        var queueMessage = retrieved.Value.Value;
        try
        {
            var handled = await messageHandlerAsync(queueMessage.Body, cancellationToken);
            if (handled.IsFailure)
            {
                await ReturnMessageToQueueForNextPopAsync(queueUrl, queueMessage, cancellationToken);

                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            await ReturnMessageToQueueForNextPopAsync(queueUrl, queueMessage, cancellationToken);

            _recorder.TraceError(null,
                ex, "Failed to handle last message: {MessageId} from queue: {Queue}", queueMessage.MessageId,
                queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }

        await MarkMessageAsHandledAsync(queueUrl, queueMessage, cancellationToken);
        return true;
    }

    public async Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AWSSQSQueueStore_MissingQueueName);
        message.ThrowIfNotValuedParameter(nameof(message),
            Resources.AWSSQSQueueStore_MissingMessage);

        var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
        if (!queueUrl.HasValue)
        {
            var created = await CreateQueueAsync(queueName, cancellationToken);
            if (created.IsFailure)
            {
                return created.Error;
            }

            queueUrl = created.Value.QueueUrl;
        }

        try
        {
            var receipt = await SendMessageAsync(queueUrl, message, cancellationToken);
            _recorder.TraceInformation(null, "Added message: {Message} to queue: {Queue}",
                receipt.MessageId, queueUrl);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to push message: {Message} to queue: {Queue}", message, queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> ClearMessagesAsync(string queueName, CancellationToken cancellationToken)
    {
        var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

        try
        {
            await _serviceClient.PurgeQueueAsync(new PurgeQueueRequest
            {
                QueueUrl = queueUrl
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to clear messages from queue: {Queue}", queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
    }
#endif
    
    public async Task<Result<QueueIdentifiers, Error>> CreateQueueAsync(string queueName,
        CancellationToken cancellationToken)
    {
        var sanitizedQueueName = queueName.SanitizeAndValidateQueueName();
        var created = await CreateDeadLetterQueueAsync(sanitizedQueueName, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var deadLetterQueueArn = created.Value.QueueArn;
        var redrivePolicy = new
        {
            DeadLetterTargetArn = deadLetterQueueArn,
            MaxReceiveCount = 10
        }.ToJson(casing: StringExtensions.JsonCasing.Camel)!;

        try
        {
            var queue = await _serviceClient.CreateQueueAsync(new CreateQueueRequest
            {
                QueueName = $"{sanitizedQueueName}.fifo",
                Attributes = new Dictionary<string, string>
                {
                    { QueueAttributeName.RedrivePolicy, redrivePolicy },
                    { QueueAttributeName.FifoQueue, "true" },
                    { QueueAttributeName.ContentBasedDeduplication, "true" }
                }
            }, cancellationToken);
            var queueUrl = queue.QueueUrl;
            if (queueUrl.HasNoValue())
            {
                throw new InvalidOperationException("Created queue has no QueueUrl");
            }

            var queueAttributes = await _serviceClient.GetQueueAttributesAsync(queueUrl, ["All"], cancellationToken);
            var queueArn = queueAttributes.QueueARN;
            if (queueArn.HasNoValue())
            {
                throw new InvalidOperationException("Queue has no QueueARN");
            }

            _knownQueueUrls.Add(sanitizedQueueName, queueUrl);

            return new QueueIdentifiers(queueUrl, queueArn);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to create queue: {Queue}", sanitizedQueueName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private async Task<Optional<string>> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken)
    {
        var sanitizedQueueName = queueName.SanitizeAndValidateQueueName();
        if (_knownQueueUrls.TryGetValue(sanitizedQueueName, out var url))
        {
            return url;
        }

        try
        {
            var response = await _serviceClient.GetQueueUrlAsync(new GetQueueUrlRequest
            {
                QueueName = $"{sanitizedQueueName}.fifo"
            }, cancellationToken);
            var queueUrl = response.QueueUrl;
            if (queueUrl.HasNoValue())
            {
                throw new InvalidOperationException("Queue has no QueueUrl");
            }

            _knownQueueUrls.Add(sanitizedQueueName, response.QueueUrl);
            return response.QueueUrl;
        }
        catch (QueueDoesNotExistException)
        {
            return Optional<string>.None;
        }
    }

    private Task<SendMessageResponse> SendMessageAsync(string queueUrl, string message,
        CancellationToken cancellationToken)
    {
        return _serviceClient.SendMessageAsync(new SendMessageRequest
        {
            MessageBody = message,
            QueueUrl = queueUrl,
            MessageGroupId = AWSConstants.FifoGroupName
        }, cancellationToken);
    }

    private async Task<Result<QueueIdentifiers, Error>> CreateDeadLetterQueueAsync(string sanitizedQueueName,
        CancellationToken cancellationToken)
    {
        try
        {
            var queue = await _serviceClient.CreateQueueAsync(new CreateQueueRequest
            {
                QueueName = $"{sanitizedQueueName}-poison.fifo",
                Attributes = new Dictionary<string, string>
                {
                    { QueueAttributeName.FifoQueue, "true" }
                }
            }, cancellationToken);
            var queueUrl = queue.QueueUrl;
            if (queueUrl.HasNoValue())
            {
                throw new InvalidOperationException("Created queue has no QueueUrl");
            }

            var queueAttributes =
                await _serviceClient.GetQueueAttributesAsync(queueUrl, ["All"], cancellationToken);
            var queueArn = queueAttributes.QueueARN;
            if (queueArn.HasNoValue())
            {
                throw new InvalidOperationException("Queue has no QueueARN");
            }

            return new QueueIdentifiers(queueUrl, queueArn);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to create dead-letter queue: {Queue}", sanitizedQueueName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private async Task<Result<Optional<Message>, Error>> GetNextMessageAsync(string queueUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1
            }, cancellationToken);
            if (response.Messages.Count > 0)
            {
                return Optional<Message>.Some(response.Messages[0]);
            }

            _recorder.TraceInformation(null, "No message on queue: {Queue}", queueUrl);
            return Optional<Message>.None;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to POP last message from queue: {Queue}", queueUrl);
            return Error.EntityNotFound();
        }
    }

    private async Task MarkMessageAsHandledAsync(string queueUrl, Message message,
        CancellationToken cancellationToken)
    {
        var request = new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        };

        try
        {
            await _serviceClient.DeleteMessageAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to remove last message: {MessageId} from queue: {Queue}", message, queueUrl);
        }
    }

    private async Task ReturnMessageToQueueForNextPopAsync(string queueUrl, Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _serviceClient.ChangeMessageVisibilityAsync(queueUrl, message.ReceiptHandle, 0, cancellationToken);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to return the current message: {MessageId} to the queue: {Queue}", message.MessageId,
                queueUrl);
        }
    }

}

public record QueueIdentifiers(string QueueUrl, string QueueArn);