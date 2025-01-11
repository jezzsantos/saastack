using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Azure.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Azure.ApplicationServices;

/// <summary>
///     Provides a queue store for Azure Storage Account Queues
/// </summary>
[UsedImplicitly]
public class AzureStorageAccountQueueStore : IQueueStore
{
    private readonly AzureStorageAccountStoreOptions.ConnectionOptions _connectionOptions;
    private readonly Dictionary<string, bool> _queueExistenceChecks = new();
    private readonly IRecorder _recorder;

    public static AzureStorageAccountQueueStore Create(IRecorder recorder, AzureStorageAccountStoreOptions options)
    {
        return new AzureStorageAccountQueueStore(recorder, options.Connection);
    }

    private AzureStorageAccountQueueStore(IRecorder recorder,
        AzureStorageAccountStoreOptions.ConnectionOptions connectionOptions)
    {
        _recorder = recorder;
        _connectionOptions = connectionOptions;
    }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AnyStore_MissingQueueName);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        var properties = await queue.GetPropertiesAsync(cancellationToken);
        return properties.NotExists()
            ? 0
            : properties.Value.ApproximateMessagesCount;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AnyStore_MissingQueueName);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        // NOTE: deleting the entire queue may take far too long (this method is only tenable in testing)
        await queue.DeleteAsync(cancellationToken);

        _queueExistenceChecks.Remove(queueName);
#else
        await Task.CompletedTask;
#endif

        return Result.Ok;
    }
#endif

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AnyStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        var retrieved = await GetNextMessageAsync(queue, cancellationToken);
        if (retrieved.IsFailure || !retrieved.Value.HasValue)
        {
            return false;
        }

        var queueMessage = retrieved.Value.Value;
        try
        {
            var handled = await messageHandlerAsync(queueMessage.MessageText, cancellationToken);
            if (handled.IsFailure)
            {
                await ReturnMessageToQueueForNextPopAsync(queue, queueMessage, cancellationToken);

                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            await ReturnMessageToQueueForNextPopAsync(queue, queueMessage, cancellationToken);

            _recorder.TraceError(null,
                ex, "Failed to handle last message: {MessageId} from queue: {Queue}", queueMessage.MessageId,
                queue.Name);
            return ex.ToError(ErrorCode.Unexpected);
        }

        await MarkMessageAsHandledAsync(queue, queueMessage, cancellationToken);
        return true;
    }

    public async Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AnyStore_MissingQueueName);
        message.ThrowIfNotValuedParameter(nameof(message),
            Resources.AnyStore_MissingMessage);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        try
        {
            var receipt = await queue.SendMessageAsync(message, cancellationToken);
            _recorder.TraceInformation(null, "Added message: {Message} to queue: {Queue}",
                receipt.Value.MessageId,
                queue.Name);
        }
        catch (RequestFailedException ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to push message: {Message} to queue: {Queue}. Error was: {ErrorCode}", message,
                queue.Name, ex.ErrorCode ?? "none");
            return ex.ToError(ErrorCode.Unexpected);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to push message: {Message} to queue: {Queue}", message, queue.Name);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
    }

    private async Task<Result<Optional<QueueMessage>, Error>> GetNextMessageAsync(QueueClient queue,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await queue.ReceiveMessageAsync(null, cancellationToken);
            if (message.HasValue)
            {
                return Optional<QueueMessage>.Some(message.Value);
            }

            _recorder.TraceInformation(null, "No message on queue: {Queue}", queue.Name);
            return Optional<QueueMessage>.None;
        }
        catch (RequestFailedException ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to POP last message from queue: {Queue}. Error was: {ErrorCode}", queue.Name,
                ex.ErrorCode ?? "none");
            return Error.EntityNotFound();
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to POP last message from queue: {Queue}", queue.Name);
            return Error.EntityNotFound();
        }
    }

    private async Task MarkMessageAsHandledAsync(QueueClient queue, QueueMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to remove last message: {MessageId} from queue: {Queue}. Error was: {ErrorCode}",
                message.MessageId,
                queue.Name, ex.ErrorCode ?? "none");
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to remove last message: {MessageId} from queue: {Queue}", message.MessageId,
                queue.Name);
        }
    }

    private async Task ReturnMessageToQueueForNextPopAsync(QueueClient queue, QueueMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await queue.UpdateMessageAsync(message.MessageId, message.PopReceipt, visibilityTimeout: TimeSpan.Zero,
                cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to return the current message: {MessageId} to the queue: {Queue}. Error was: {ErrorCode}",
                message.MessageId, queue.Name, ex.ErrorCode ?? "none");
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to return the current message: {MessageId} to the queue: {Queue}", message.MessageId,
                queue.Name);
        }
    }

    private async Task<QueueClient> ConnectToQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var sanitizedQueueName = queueName.SanitizeAndValidateStorageAccountResourceName();
        var queueClientOptions = new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };
        QueueClient queue;
        switch (_connectionOptions.Type)
        {
            case AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.Credentials:
                queue = new QueueClient(_connectionOptions.ConnectionString, sanitizedQueueName, queueClientOptions);
                break;

            case AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.ManagedIdentity:
            {
                var uri = new Uri(
                    $"https://{_connectionOptions.AccountName}.queue.core.windows.net/{sanitizedQueueName}");
                queue = new QueueClient(uri, _connectionOptions.Credential, queueClientOptions);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType));
        }

        if (IsQueueExistenceCheckPerformed(sanitizedQueueName))
        {
            return queue;
        }

        var exists = await queue.ExistsAsync(cancellationToken);
        if (!exists)
        {
            await queue.CreateAsync(null, cancellationToken);
        }

        return queue;
    }

    private bool IsQueueExistenceCheckPerformed(string queueName)
    {
        _queueExistenceChecks.TryAdd(queueName, false);
        if (_queueExistenceChecks[queueName])
        {
            return true;
        }

        _queueExistenceChecks[queueName] = true;

        return false;
    }
}