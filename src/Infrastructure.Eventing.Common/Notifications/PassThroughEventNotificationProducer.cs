using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a producer of notification events that simply passes on the published event
/// </summary>
public sealed class PassThroughEventNotificationProducer<TAggregateRoot> : IEventNotificationProducer
    where TAggregateRoot : IEventSourcedAggregateRoot
{
    public Type RootAggregateType => typeof(TAggregateRoot);

    public Task<Result<Optional<IDomainEvent>, Error>> PublishAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<Optional<IDomainEvent>, Error>>(new Optional<IDomainEvent>(changeEvent));
    }
}