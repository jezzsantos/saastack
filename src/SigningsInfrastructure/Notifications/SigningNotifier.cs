using Infrastructure.Eventing.Common.Notifications;
using SigningsDomain;

namespace SigningsInfrastructure.Notifications;

public class SigningNotifier : NoOpEventNotificationRegistration<SigningRequestRoot>
{
}