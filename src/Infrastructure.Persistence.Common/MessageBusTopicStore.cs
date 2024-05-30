using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to messages on an FIFO bus topic
/// </summary>
public sealed class MessageBusTopicStore<TMessage> : IMessageBusTopicStore<TMessage>
    where TMessage : IQueuedMessage, new()
{
    private readonly IMessageBusStore _messageBusStore;
    private readonly IMessageQueueIdFactory _messageQueueIdFactory;
    private readonly IRecorder _recorder;
    private readonly string _topicName;

    public MessageBusTopicStore(IRecorder recorder, string topicName, IMessageQueueIdFactory messageQueueIdFactory,
        IMessageBusStore messageBusStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _messageQueueIdFactory = messageQueueIdFactory;
        _messageBusStore = messageBusStore;
        _topicName = topicName;
    }

    public Guid InstanceId { get; }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(string subscriptionName, CancellationToken cancellationToken)
    {
        return await _messageBusStore.CountAsync(_topicName, subscriptionName, cancellationToken);
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _messageBusStore.DestroyAllAsync(_topicName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All messages were deleted from the topic {Topic} in the {Store} store",
                _topicName, _messageBusStore.GetType().Name);
        }

        return deleted;
    }
#endif

#if TESTINGONLY
    public async Task<Result<bool, Error>> ReceiveSingleAsync(string subscriptionName,
        Func<TMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        TMessage message;
        return await _messageBusStore.ReceiveSingleAsync(_topicName, subscriptionName,
            async (messageAsText, cancellation) =>
            {
                if (messageAsText.HasValue())
                {
                    message = messageAsText.FromJson<TMessage>()!;
                    var handled = await onMessageReceivedAsync(message, cancellation);
                    if (handled.IsFailure)
                    {
                        return handled.Error;
                    }

                    _recorder.TraceDebug(null, "Message {Text} was removed from the queue {Queue} in the {Store} store",
                        messageAsText,
                        _topicName, _messageBusStore.GetType().Name);
                }

                return Result.Ok;
            }, cancellationToken);
    }
#endif

    public async Task<Result<TMessage, Error>> SendAsync(ICallContext call, TMessage message,
        CancellationToken cancellationToken)
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

        var pushed = await _messageBusStore.SendAsync(_topicName, messageJson, cancellationToken);
        if (pushed.IsFailure)
        {
            return pushed.Error;
        }

        _recorder.TraceDebug(null, "Message {Message} was added to the queue {Queue} in the {Store} store", messageJson,
            _topicName, _messageBusStore.GetType().Name);

        return message;
    }

    private string CreateMessageId()
    {
        return _messageQueueIdFactory.Create(_topicName);
    }
}