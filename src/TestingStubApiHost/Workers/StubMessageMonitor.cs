using System.Collections.Concurrent;
using Common;
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace TestingStubApiHost.Workers;

/// <summary>
///     Provides a monitor that detects when queued messages arrive on any queues or any topics of a message bus
/// </summary>
public class StubMessageMonitor : IMessageMonitor
{
    private readonly ConcurrentQueue<string> _queueNames = new();
    private readonly ConcurrentQueue<string> _topicNames = new();

    public Optional<string> NextQueueName()
    {
        if (!_queueNames.TryDequeue(out var queueName))
        {
            return Optional<string>.None;
        }

        return queueName;
    }

    public Optional<string> NextTopicName()
    {
        if (!_topicNames.TryDequeue(out var topicName))
        {
            return Optional<string>.None;
        }

        return topicName;
    }

    public void NotifyQueueMessagesChanged(string queueName, int messageCount)
    {
        if (!_queueNames.Contains(queueName))
        {
            _queueNames.Enqueue(queueName);
        }
    }

    public void NotifyTopicMessagesChanged(string topicName, int messageCount)
    {
        if (!_topicNames.Contains(topicName))
        {
            _topicNames.Enqueue(topicName);
        }
    }
}