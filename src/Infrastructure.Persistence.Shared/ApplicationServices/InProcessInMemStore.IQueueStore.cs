#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

public partial class InProcessInMemStore : IQueueStore, IQueueStoreTrigger
{
    private readonly Dictionary<string, Dictionary<string, HydrationProperties>> _queues = new();

    Task<Result<long, Error>> IQueueStore.CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.InProcessInMemDataStore_MissingQueueName);

        if (_queues.TryGetValue(queueName, out var value))
        {
            return Task.FromResult<Result<long, Error>>(value.Count);
        }

        return Task.FromResult<Result<long, Error>>(0);
    }

    Task<Result<Error>> IQueueStore.DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.InProcessInMemDataStore_MissingQueueName);

        if (_queues.ContainsKey(queueName))
        {
            _queues.Remove(queueName);
        }
#endif

        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.InProcessInMemDataStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        if (_queues.ContainsKey(queueName)
            && _queues[queueName].Any())
        {
            var fifoMessage = _queues[queueName].MinBy(x => x.Key);
            var message = fifoMessage.Value["Message"].ToString();
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

            _queues[queueName].Remove(fifoMessage.Key);
            return true;
        }

        return false;
    }

    public Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.InProcessInMemDataStore_MissingQueueName);
        message.ThrowIfNotValuedParameter(nameof(message), Resources.InProcessInMemDataStore_MissingQueueMessage);

        if (!_queues.ContainsKey(queueName))
        {
            _queues.Add(queueName, new Dictionary<string, HydrationProperties>());
        }

        var messageId = DateTime.UtcNow.Ticks.ToString();
        _queues[queueName]
            .Add(messageId, new HydrationProperties
            {
                { "Message", message }
            });

        FireMessageQueueUpdated?.Invoke(this, new MessagesQueueUpdatedArgs(queueName, _queues[queueName].Count));

        return Task.FromResult(Result.Ok);
    }

    public event MessageQueueUpdated? FireMessageQueueUpdated;

    private void NotifyAllQueuedMessages()
    {
        if (_queues.HasNone() || FireMessageQueueUpdated.NotExists())
        {
            return;
        }

        foreach (var (queueName, messages) in _queues)
        {
            var messageCount = messages.Count;
            if (messageCount > 0)
            {
                FireMessageQueueUpdated?.Invoke(this, new MessagesQueueUpdatedArgs(queueName, messageCount));
            }
        }
    }
}
#endif