using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

public class EmailMessageQueue : IEmailMessageQueue
{
    private readonly MessageQueueStore<EmailMessage> _messageQueue;

    public EmailMessageQueue(IRecorder recorder, IMessageQueueIdFactory messageQueueIdFactory, IQueueStore store)
    {
        _messageQueue = new MessageQueueStore<EmailMessage>(recorder, messageQueueIdFactory, store);
    }

    public Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return _messageQueue.CountAsync(cancellationToken);
    }

    public Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return _messageQueue.DestroyAllAsync(cancellationToken);
    }

    public Task<Result<bool, Error>> PopSingleAsync(
        Func<EmailMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        return _messageQueue.PopSingleAsync(onMessageReceivedAsync, cancellationToken);
    }

    public Task<Result<EmailMessage, Error>> PushAsync(ICallContext call, EmailMessage message,
        CancellationToken cancellationToken)
    {
        return _messageQueue.PushAsync(call, message, cancellationToken);
    }

    Task<Result<Error>> IApplicationRepository.DestroyAllAsync(CancellationToken cancellationToken)
    {
        return DestroyAllAsync(cancellationToken);
    }
}