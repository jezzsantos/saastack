namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines the registration information for both a domain and integration consumers
/// </summary>
public interface IEventNotificationRegistration
{
    /// <summary>
    ///     Returns the consumers of domain events
    /// </summary>
    List<IDomainEventNotificationConsumer> DomainEventConsumers { get; }

    /// <summary>
    ///     Returns the translator of integration events
    /// </summary>
    IIntegrationEventNotificationTranslator IntegrationEventTranslator { get; }
}