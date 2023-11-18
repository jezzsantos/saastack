#if TESTINGONLY
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace Infrastructure.Web.Hosting.Common.ApplicationServices;

/// <summary>
///     Provides a testing stub handler for dealing with message queue update notifications
/// </summary>
public class StubQueueStoreNotificationHandler : IQueueStoreNotificationHandler
{
    private readonly IMonitoredMessageQueues _monitoredMessageQueues;

    public StubQueueStoreNotificationHandler(IMonitoredMessageQueues monitoredMessageQueues)
    {
        _monitoredMessageQueues = monitoredMessageQueues;
    }

    public void HandleMessagesQueueUpdated(string queueName, int messageCount)
    {
        _monitoredMessageQueues.AddQueueName(queueName);
    }
}
#endif