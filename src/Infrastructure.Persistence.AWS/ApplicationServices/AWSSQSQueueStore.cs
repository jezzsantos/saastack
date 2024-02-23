using Amazon.SQS;
using Amazon.SQS.Model;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Infrastructure.Persistence.AWS.Extensions;
using Infrastructure.Persistence.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.AWS.ApplicationServices;

/// <summary>
///     Provides a queue store for AWS Simple Queue Service (SQS) Queues
/// </summary>
public class AWSSQSQueueStore : IQueueStore
{
    private readonly Dictionary<string, string> _knownQueueUrls;
    private readonly IRecorder _recorder;
    private readonly IAmazonSQS _serviceClient;

    public static AWSSQSQueueStore Create(IRecorder recorder, IConfigurationSettings settings)
    {
        var (credentials, regionEndpoint) = settings.GetConnection();
        if (regionEndpoint.Exists())
        {
            var remoteClient = new AmazonSQSClient(credentials, regionEndpoint);
            return new AWSSQSQueueStore(recorder, remoteClient);
        }

        var localStackClient = new AmazonSQSClient(credentials,
            new AmazonSQSConfig { ServiceURL = AWSConstants.LocalStackServiceUrl });

        return new AWSSQSQueueStore(recorder, localStackClient);
    }

    private AWSSQSQueueStore(IRecorder recorder, IAmazonSQS serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _knownQueueUrls = new Dictionary<string, string>();
    }

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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to retrieve attributes from queue: {Queue}", queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
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
        if (!retrieved.IsSuccessful || !retrieved.Value.HasValue)
        {
            return false;
        }

        var queueMessage = retrieved.Value.Value;
        try
        {
            var handled = await messageHandlerAsync(queueMessage.Body, cancellationToken);
            if (!handled.IsSuccessful)
            {
                await ReturnMessageToQueueForNextPopAsync(queueUrl, queueMessage, cancellationToken);

                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            await ReturnMessageToQueueForNextPopAsync(queueUrl, queueMessage, cancellationToken);

            _recorder.Crash(null,
                CrashLevel.NonCritical,
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
            if (!created.IsSuccessful)
            {
                return created.Error;
            }

            queueUrl = created.Value;
        }

        try
        {
            var receipt = await SendMessageAsync(queueUrl, message, cancellationToken);
            _recorder.TraceInformation(null, "Added message: {Message} to queue: {Queue}",
                receipt.MessageId, queueUrl);
        }
        catch (Exception ex)
        {
            _recorder.Crash(null, CrashLevel.NonCritical,
                ex, "Failed to push message: {Message} to queue: {Queue}", message, queueUrl);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
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
            var response = await _serviceClient.GetQueueUrlAsync(sanitizedQueueName, cancellationToken);
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
            QueueUrl = queueUrl
        }, cancellationToken);
    }

    private async Task<Result<string, Error>> CreateQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var sanitizedQueueName = queueName.SanitizeAndValidateQueueName();
        var created = await CreateDeadLetterQueueAsync(sanitizedQueueName, cancellationToken);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var deadLetterQueueArn = created.Value;
        var redrivePolicy = new
        {
            DeadLetterTargetArn = deadLetterQueueArn,
            MaxReceiveCount = 10
        }.ToJson(casing: StringExtensions.JsonCasing.Camel)!;

        try
        {
            var queue = await _serviceClient.CreateQueueAsync(new CreateQueueRequest
            {
                QueueName = sanitizedQueueName,
                Attributes = new Dictionary<string, string>
                {
                    { "RedrivePolicy", redrivePolicy }
                }
            }, cancellationToken);
            var queueUrl = queue.QueueUrl;
            if (queueUrl.HasNoValue())
            {
                throw new InvalidOperationException("Created queue has no QueueUrl");
            }

            _knownQueueUrls.Add(sanitizedQueueName, queue.QueueUrl);

            return queue.QueueUrl;
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to create queue: {Queue}", sanitizedQueueName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private async Task<Result<string, Error>> CreateDeadLetterQueueAsync(string sanitizedQueueName,
        CancellationToken cancellationToken)
    {
        var deadLetterQueueName = $"{sanitizedQueueName}-poison";

        try
        {
            var queue = await _serviceClient.CreateQueueAsync(deadLetterQueueName, cancellationToken);
            var queueAttributes = await _serviceClient.GetQueueAttributesAsync(queue.QueueUrl, new List<string>
            {
                "All"
            }, cancellationToken);
            var queueArn = queueAttributes.QueueARN;
            if (queueArn.HasNoValue())
            {
                throw new InvalidOperationException("Queue has no QueueARN");
            }

            return queueArn;
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
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
            _recorder.Crash(null, CrashLevel.NonCritical,
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
            _recorder.Crash(null, CrashLevel.NonCritical, ex,
                "Failed to remove last message: {MessageId} from queue: {Queue}", message, queueUrl);
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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to return the current message: {MessageId} to the queue: {Queue}", message.MessageId,
                queueUrl);
        }
    }
}