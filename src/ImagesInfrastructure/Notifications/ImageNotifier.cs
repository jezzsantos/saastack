using ImagesDomain;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace ImagesInfrastructure.Notifications;

public class ImageNotifier : IEventNotificationRegistration
{
    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new NoOpIntegrationEventNotificationTranslator<ImageRoot>();
}