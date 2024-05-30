using Infrastructure.Eventing.Common.Notifications;
using UserProfilesDomain;

namespace UserProfilesInfrastructure.Notifications;

public class UserProfileNotifier : NoOpEventNotificationRegistration<UserProfileRoot>
{
}