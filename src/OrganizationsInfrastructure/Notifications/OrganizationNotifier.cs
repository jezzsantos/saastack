using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Notifications;

public class OrganizationNotifier : IEventNotificationRegistration
{
    public OrganizationNotifier(IEnumerable<IDomainEventNotificationConsumer> consumers)
    {
        DomainEventConsumers = consumers.ToList();
    }

    public List<IDomainEventNotificationConsumer> DomainEventConsumers { get; }

    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new NoOpIntegrationEventNotificationTranslator<OrganizationRoot>();
}