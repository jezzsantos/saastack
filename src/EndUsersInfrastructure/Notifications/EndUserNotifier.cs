using EndUsersDomain;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace EndUsersInfrastructure.Notifications;

public class EndUserNotifier : IEventNotificationRegistration
{
    public EndUserNotifier(IEnumerable<IDomainEventNotificationConsumer> domainConsumers)
    {
        DomainEventConsumers = domainConsumers.ToList();
    }

    public List<IDomainEventNotificationConsumer> DomainEventConsumers { get; }

    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new EndUserIntegrationEventNotificationTranslator<EndUserRoot>();
}