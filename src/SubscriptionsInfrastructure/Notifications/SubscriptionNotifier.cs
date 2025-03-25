using Infrastructure.Eventing.Common.Notifications;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Notifications;

public class SubscriptionNotifier : NoOpEventNotificationRegistration<SubscriptionRoot>
{
}