namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines the registration information for both a notifications producer and a notifications consumer
/// </summary>
public interface IEventNotificationRegistration
{
    /// <summary>
    ///     Returns the consumer of the events
    /// </summary>
    IEventNotificationConsumer Consumer { get; }

    /// <summary>
    ///     Returns the producer of the events
    /// </summary>
    IEventNotificationProducer Producer { get; }
}