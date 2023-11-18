#if TESTINGONLY
using System.Collections.Concurrent;
using Common;
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace Infrastructure.Web.Hosting.Common.ApplicationServices;

/// <summary>
///     Provides message queues to monitor
/// </summary>
public class MonitoredMessageQueues : IMonitoredMessageQueues
{
    private readonly ConcurrentQueue<string> _queueNames = new();

    public void AddQueueName(string queueName)
    {
        if (!_queueNames.Contains(queueName))
        {
            _queueNames.Enqueue(queueName);
        }
    }

    public Optional<string> NextQueueName()
    {
        if (!_queueNames.TryDequeue(out var queueName))
        {
            return Optional<string>.None;
        }

        return queueName;
    }
}
#endif