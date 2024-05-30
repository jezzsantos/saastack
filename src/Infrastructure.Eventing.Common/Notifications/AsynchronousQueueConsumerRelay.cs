using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a <see cref="IDomainEventConsumerRelay" /> that asynchronously notifies consumers of domain events via a
///     message queue
/// </summary>
public class AsynchronousQueueConsumerRelay : IDomainEventConsumerRelay
{
    private readonly IDomainEventingMessageBusTopic _messageBusTopic;

    public AsynchronousQueueConsumerRelay(IRecorder recorder, IMessageQueueIdFactory messageQueueIdFactory,
        IMessageBusStore store) : this(new DomainEventingMessageBusTopic(recorder, messageQueueIdFactory, store))
    {
    }

    internal AsynchronousQueueConsumerRelay(IDomainEventingMessageBusTopic messageBusTopic)
    {
        _messageBusTopic = messageBusTopic;
    }

    public async Task<Result<Error>> RelayDomainEventAsync(IDomainEvent @event,
        EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var message = new DomainEventingMessage
        {
            Event = changeEvent
        };

        var call = CallContext.CreateUnknown();
        var queued = await _messageBusTopic.SendAsync(call, message, cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error
                .Wrap(ErrorCode.Unexpected,
                    Resources.AsynchronousConsumerRelay_RelayFailed.Format(GetType().Name, @event.RootId,
                        changeEvent.Metadata.Fqn));
        }

        return Result.Ok;
    }
}