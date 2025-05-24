using Application.Interfaces.Services;
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
    private readonly IHostSettings _hostSettings;
    private readonly IDomainEventingMessageBusTopic _messageBusTopic;

    public AsynchronousQueueConsumerRelay(IRecorder recorder, IHostSettings hostSettings,
        IMessageBusTopicMessageIdFactory messageBusTopicMessageIdFactory,
        IMessageBusStore store) : this(
        new DomainEventingMessageBusTopic(recorder, messageBusTopicMessageIdFactory, store), hostSettings)
    {
    }

    internal AsynchronousQueueConsumerRelay(IDomainEventingMessageBusTopic messageBusTopic,
        IHostSettings hostSettings)
    {
        _messageBusTopic = messageBusTopic;
        _hostSettings = hostSettings;
    }

    public async Task<Result<Error>> RelayDomainEventAsync(IDomainEvent @event,
        EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var message = new DomainEventingMessage
        {
            Event = changeEvent
        };

        var region = _hostSettings.GetRegion();
        var call = CallContext.CreateUnknown(region);
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