using Infrastructure.Eventing.Common.Notifications;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Persistence.Notifications;

public class SubscriptionNotifier : NoOpEventNotificationRegistration<SubscriptionRoot>
{
}