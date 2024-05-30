using EndUsersDomain;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace EndUsersInfrastructure.Notifications;

public class EndUserNotifier : IEventNotificationRegistration
{
    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new EndUserNotificationTranslator<EndUserRoot>();
}