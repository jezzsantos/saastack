using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
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
    private readonly IHostRegionService _hostRegionService;

    public AsynchronousQueueConsumerRelay(IRecorder recorder, IHostRegionService hostRegionService,
        IMessageBusTopicMessageIdFactory messageBusTopicMessageIdFactory,
        IMessageBusStore store) : this(
        new DomainEventingMessageBusTopic(recorder, messageBusTopicMessageIdFactory, store), hostRegionService)
    {
    }

    internal AsynchronousQueueConsumerRelay(IDomainEventingMessageBusTopic messageBusTopic,
        IHostRegionService hostRegionService)
    {
        _messageBusTopic = messageBusTopic;
        _hostRegionService = hostRegionService;
    }

    public async Task<Result<Error>> RelayDomainEventAsync(IDomainEvent @event,
        EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var message = new DomainEventingMessage
        {
            Event = changeEvent
        };

        var region = _hostRegionService.GetRegion();
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