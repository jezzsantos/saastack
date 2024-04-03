using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;
using UserProfilesDomain;

namespace UserProfilesInfrastructure.Notifications;

public class UserProfileNotifier : IEventNotificationRegistration
{
    public UserProfileNotifier(IEnumerable<IDomainEventNotificationConsumer> consumers)
    {
        DomainEventConsumers = consumers.ToList();
    }

    public List<IDomainEventNotificationConsumer> DomainEventConsumers { get; }

    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new NoOpIntegrationEventNotificationTranslator<UserProfileRoot>();
}