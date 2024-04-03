using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides the registration information for both consumers of domain and integration events
/// </summary>
public sealed class EventNotificationRegistration : IEventNotificationRegistration
{
    public EventNotificationRegistration(IIntegrationEventNotificationTranslator translator,
        List<IDomainEventNotificationConsumer> domainEventConsumers)
    {
        DomainEventConsumers = domainEventConsumers;
        IntegrationEventTranslator = translator;
    }

    public IIntegrationEventNotificationTranslator IntegrationEventTranslator { get; }

    public List<IDomainEventNotificationConsumer> DomainEventConsumers { get; }
}