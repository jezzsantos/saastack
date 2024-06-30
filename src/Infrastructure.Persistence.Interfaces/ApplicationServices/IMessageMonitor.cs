using Common;

namespace Infrastructure.Persistence.Interfaces.ApplicationServices;

/// <summary>
///     Monitors the arrival of new messages on queues and message buses
/// </summary>
public interface IMessageMonitor
{
    Optional<string> NextQueueName();

    Optional<string> NextTopicName();

    void NotifyQueueMessagesChanged(string queueName, int messageCount);

    void NotifyTopicMessagesChanged(string topicName, int messageCount);
}