using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a registration for a consumer that handles all events but does nothing with them
/// </summary>
public sealed class NoOpConsumerRegistration<TAggregateRoot> : IEventNotificationRegistration
    where TAggregateRoot : IEventSourcedAggregateRoot
{
    public IEventNotificationProducer Producer => new PassThroughEventNotificationProducer<TAggregateRoot>();

    public IEventNotificationConsumer Consumer => new NoOpConsumer();
}