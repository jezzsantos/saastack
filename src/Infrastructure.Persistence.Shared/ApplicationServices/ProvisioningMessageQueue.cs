using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

public class ProvisioningMessageQueue : IProvisioningMessageQueue
{
    private readonly MessageQueueStore<ProvisioningMessage> _messageQueue;

    public ProvisioningMessageQueue(IRecorder recorder, IMessageQueueIdFactory messageQueueIdFactory, IQueueStore store)
    {
        _messageQueue = new MessageQueueStore<ProvisioningMessage>(recorder, messageQueueIdFactory, store);
    }

#if TESTINGONLY
    public Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return _messageQueue.CountAsync(cancellationToken);
    }
#endif

#if TESTINGONLY
    public Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return _messageQueue.DestroyAllAsync(cancellationToken);
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IApplicationRepository.DestroyAllAsync(CancellationToken cancellationToken)
    {
        return DestroyAllAsync(cancellationToken);
    }
#endif

    public Task<Result<bool, Error>> PopSingleAsync(
        Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        return _messageQueue.PopSingleAsync(onMessageReceivedAsync, cancellationToken);
    }

    public Task<Result<ProvisioningMessage, Error>> PushAsync(ICallContext call, ProvisioningMessage message,
        CancellationToken cancellationToken)
    {
        return _messageQueue.PushAsync(call, message, cancellationToken);
    }
}