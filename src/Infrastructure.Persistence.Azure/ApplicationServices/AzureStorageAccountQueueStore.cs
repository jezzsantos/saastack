using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
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
    private const string AccountKeySettingName = "ApplicationServices:Persistence:AzureStorageAccount:AccountKey";
    private const string AccountNameSettingName = "ApplicationServices:Persistence:AzureStorageAccount:AccountName";
    private const string ConnectionString =
        "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";
    private const string DefaultConnectionString = "UseDevelopmentStorage=true";
    private readonly string _connectionString;
    private readonly Dictionary<string, bool> _queueExistenceChecks = new();
    private readonly IRecorder _recorder;

    public static AzureStorageAccountQueueStore Create(IRecorder recorder, ISettings settings)
    {
        var accountKey = settings.GetString(AccountKeySettingName);
        var accountName = settings.GetString(AccountNameSettingName);
        var connection = accountKey.HasValue()
            ? ConnectionString.Format(accountName, accountKey)
            : DefaultConnectionString;

        return new AzureStorageAccountQueueStore(recorder, connection);
    }

    private AzureStorageAccountQueueStore(IRecorder recorder, string connectionString)
    {
        _recorder = recorder;
        _connectionString = connectionString;
    }

    public async Task<Result<long, Error>> CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AzureStorageAccountQueueStore_MissingQueueName);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        var properties = await queue.GetPropertiesAsync(cancellationToken);
        return properties.NotExists()
            ? 0
            : properties.Value.ApproximateMessagesCount;
    }

    public async Task<Result<Error>> DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AzureStorageAccountQueueStore_MissingQueueName);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        // NOTE: deleting the entire queue may take far too long (this method is only tenable in testing)
        await queue.DeleteAsync(cancellationToken);

        _queueExistenceChecks.Remove(queueName);
#endif

        return Result.Ok;
    }

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AzureStorageAccountQueueStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var queue = await ConnectToQueueAsync(queueName, cancellationToken);

        var retrieved = await GetNextMessageAsync(queue, cancellationToken);
        if (!retrieved.IsSuccessful || !retrieved.Value.HasValue)
        {
            return false;
        }

        var queueMessage = retrieved.Value.Value;
        try
        {
            var handled = await messageHandlerAsync(queueMessage.MessageText, cancellationToken);
            if (!handled.IsSuccessful)
            {
                await ReturnMessageToQueueForNextPopAsync(queue, queueMessage, cancellationToken);

                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            await ReturnMessageToQueueForNextPopAsync(queue, queueMessage, cancellationToken);

            _recorder.Crash(null,
                CrashLevel.NonCritical,
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
            Resources.AzureStorageAccountQueueStore_MissingQueueName);
        message.ThrowIfNotValuedParameter(nameof(message),
            Resources.AzureStorageAccountQueueStore_MissingQueueName);

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
            _recorder.Crash(null, CrashLevel.NonCritical,
                ex, "Failed to push message: {Message} to queue: {Queue}. Error was: {ErrorCode}", message,
                queue.Name, ex.ErrorCode ?? "none");
        }
        catch (Exception ex)
        {
            _recorder.Crash(null, CrashLevel.NonCritical,
                ex, "Failed to push message: {Message} to queue: {Queue}", message, queue.Name);
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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to POP last message from queue: {Queue}. Error was: {ErrorCode}", queue.Name,
                ex.ErrorCode ?? "none");
            return Error.EntityNotFound();
        }
        catch (Exception ex)
        {
            _recorder.Crash(null, CrashLevel.NonCritical,
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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to remove last message: {MessageId} from queue: {Queue}. Error was: {ErrorCode}",
                message.MessageId,
                queue.Name, ex.ErrorCode ?? "none");
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to return the current message: {MessageId} to the queue: {Queue}. Error was: {ErrorCode}",
                message.MessageId, queue.Name, ex.ErrorCode ?? "none");
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to return the current message: {MessageId} to the queue: {Queue}", message.MessageId,
                queue.Name);
        }
    }

    private async Task<QueueClient> ConnectToQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var sanitisedQueueName = queueName.SanitiseAndValidateStorageAccountResourceName();
        var queue = new QueueClient(_connectionString, sanitisedQueueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });

        if (IsQueueExistenceCheckPerformed(sanitisedQueueName))
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