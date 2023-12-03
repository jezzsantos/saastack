using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides the registration information for both a producer and consumer
/// </summary>
public sealed class EventNotificationRegistration : IEventNotificationRegistration
{
    public EventNotificationRegistration(IEventNotificationProducer producer,
        IEventNotificationConsumer consumer)
    {
        Producer = producer;
        Consumer = consumer;
    }

    public IEventNotificationProducer Producer { get; }

    public IEventNotificationConsumer Consumer { get; }
}