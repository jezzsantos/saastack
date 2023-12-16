using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to messages on a queue
/// </summary>
public sealed class MessageQueueStore<TMessage> : IMessageQueueStore<TMessage>
    where TMessage : IQueuedMessage, new()
{
    private readonly string _queueName;
    private readonly IQueueStore _queueStore;
    private readonly IRecorder _recorder;

    public MessageQueueStore(IRecorder recorder, IQueueStore queueStore)
    {
        _recorder = recorder;
        _queueStore = queueStore;
        _queueName = typeof(TMessage).GetEntityNameSafe();
    }

    public async Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return await _queueStore.CountAsync(_queueName, cancellationToken);
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _queueStore.DestroyAllAsync(_queueName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All messages were deleted from the queue {Queue} in the {Store} store",
                _queueName, _queueStore.GetType().Name);
        }

        return deleted;
    }

    public async Task<Result<bool, Error>> PopSingleAsync(
        Func<TMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        TMessage message;
        return await _queueStore.PopSingleAsync(_queueName, async (messageAsText, cancellation) =>
        {
            if (messageAsText.HasValue())
            {
                message = messageAsText.FromJson<TMessage>()!;
                var handled = await onMessageReceivedAsync(message, cancellation);
                if (!handled.IsSuccessful)
                {
                    return handled.Error;
                }

                _recorder.TraceDebug(null, "Message {Text} was removed from the queue {Queue} in the {Store} store",
                    messageAsText,
                    _queueName, _queueStore.GetType().Name);
            }

            return Result.Ok;
        }, cancellationToken);
    }

    public async Task<Result<Error>> PushAsync(ICallContext call, TMessage message, CancellationToken cancellationToken)
    {
        message.TenantId = message.TenantId.HasValue()
            ? message.TenantId
            : call.TenantId;
        message.CallId = message.CallId.HasValue()
            ? message.CallId
            : call.CallId;
        message.CallerId = message.CallerId.HasValue()
            ? message.CallerId
            : call.CallerId;
        message.MessageId = message.MessageId ?? CreateMessageId();
        var messageJson = message.ToJson()!;

        var pushed = await _queueStore.PushAsync(_queueName, messageJson, cancellationToken);
        if (!pushed.IsSuccessful)
        {
            return pushed.Error;
        }

        _recorder.TraceDebug(null, "Message {Message} was added to the queue {Queue} in the {Store} store", messageJson,
            _queueName, _queueStore.GetType().Name);

        return Result.Ok;
    }

    private string CreateMessageId()
    {
        return $"{_queueName}_{Guid.NewGuid():N}";
    }
}