using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

public class AuditMessageQueueRepository : IAuditMessageQueueRepository
{
    private readonly MessageQueueStore<AuditMessage> _messageQueue;

    public AuditMessageQueueRepository(IRecorder recorder, IHostSettings hostSettings,
        IMessageQueueMessageIdFactory messageQueueMessageIdFactory,
        IQueueStore store)
    {
        _messageQueue =
            new MessageQueueStore<AuditMessage>(recorder, hostSettings, messageQueueMessageIdFactory, store);
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
        Func<AuditMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        return _messageQueue.PopSingleAsync(onMessageReceivedAsync, cancellationToken);
    }

    public Task<Result<AuditMessage, Error>> PushAsync(ICallContext call, AuditMessage message,
        CancellationToken cancellationToken)
    {
        return _messageQueue.PushAsync(call, message, cancellationToken);
    }
}