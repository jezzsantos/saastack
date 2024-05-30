using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Shared.ApplicationServices;

public class DomainEventConsumerService : IDomainEventConsumerService
{
    private readonly List<IDomainEventNotificationConsumer> _consumers;
    private readonly IEventSourcedChangeEventMigrator _migrator;

    public DomainEventConsumerService(IEnumerable<IDomainEventNotificationConsumer> consumers,
        IEventSourcedChangeEventMigrator migrator)
    {
        _migrator = migrator;
        _consumers = consumers.ToList();
    }

    public async Task<Result<Error>> NotifyAsync(EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        var converted = changeEvent.ToDomainEvent(_migrator);
        if (converted.IsFailure)
        {
            return converted.Error;
        }

        //HACK: We are round-robin distributing these events,
        // but if it fails even once from any consumer, we will enter a retry loop
        // but events that were previously successful, will be replayed again next time around!
        var domainEvent = converted.Value;
        foreach (var consumer in _consumers)
        {
            var result = await consumer.NotifyAsync(domainEvent, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error
                    .Wrap(ErrorCode.Unexpected,
                        Resources.DomainEventConsumerService_ConsumerFailed.Format(consumer.GetType().Name,
                            domainEvent.RootId, changeEvent.Metadata.Fqn));
            }
        }

        return Result.Ok;
    }
}