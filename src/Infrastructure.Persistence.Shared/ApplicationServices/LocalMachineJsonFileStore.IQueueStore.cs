#if TESTINGONLY
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

/// <summary>
///     Defines a file repository on the local machine, that stores each entity as raw JSON.
///     store is located in named folders under the <see cref="_rootPath" />
/// </summary>
public partial class LocalMachineJsonFileStore : IQueueStore, IQueueStoreTrigger
{
    private const string QueueStoreContainerName = "Queues";

    Task<Result<long, Error>> IQueueStore.CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.InProcessInMemDataStore_MissingQueueName);

        var container = EnsureContainer(GetQueueStoreContainerPath(queueName));

        return Task.FromResult<Result<long, Error>>(container.Count);
    }

    Task<Result<Error>> IQueueStore.DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.InProcessInMemDataStore_MissingQueueName);

        var queueStore = EnsureContainer(GetQueueStoreContainerPath(queueName));
        queueStore.Erase();
#endif

        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.InProcessInMemDataStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var container = EnsureContainer(GetQueueStoreContainerPath(queueName));
        if (!container.IsEmpty())
        {
            var fifoMessageId = container
                .GetEntityIds()
                .OrderBy(x => x)
                .First();
            var firstMessage = container.Read(fifoMessageId);
            var message = firstMessage["Message"];
            try
            {
                var handled = await messageHandlerAsync(message, cancellationToken);
                if (!handled.IsSuccessful)
                {
                    return handled.Error;
                }
            }
            catch (Exception ex)
            {
                return ex.ToError(ErrorCode.Unexpected);
            }

            container.Remove(fifoMessageId);
            return true;
        }

        return false;
    }

    public Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.InProcessInMemDataStore_MissingQueueName);
        message.ThrowIfNotValuedParameter((string)nameof(message),
            Resources.InProcessInMemDataStore_MissingQueueMessage);

        var messageId = DateTime.UtcNow.Ticks.ToString();
        var container = EnsureContainer(GetQueueStoreContainerPath(queueName));
        container.Write(messageId, new Dictionary<string, Optional<string>>
        {
            { "Message", message }
        });

        FireMessageQueueUpdated?.Invoke(this, new MessagesQueueUpdatedArgs(queueName, (int)container.Count));

        return Task.FromResult(Result.Ok);
    }

    public event MessageQueueUpdated? FireMessageQueueUpdated;

    private static string GetQueueStoreContainerPath(string? containerName = null, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{QueueStoreContainerName}/{containerName}/{entityId}";
        }

        if (containerName.HasValue())
        {
            return $"{QueueStoreContainerName}/{containerName}";
        }

        return $"{QueueStoreContainerName}";
    }

    private void NotifyAllQueuedMessages()
    {
        var container = EnsureContainer(GetQueueStoreContainerPath());
        if (!container.Containers().Any())
        {
            return;
        }

        if (FireMessageQueueUpdated.NotExists())
        {
            return;
        }

        foreach (var queue in container.Containers())
        {
            var messageCount = (int)queue.Count;
            if (messageCount > 0)
            {
                FireMessageQueueUpdated?.Invoke(this, new MessagesQueueUpdatedArgs(queue.Name, messageCount));
            }
        }
    }
}
#endif