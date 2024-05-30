#if TESTINGONLY
using Common.Extensions;

namespace Infrastructure.Persistence.Interfaces.ApplicationServices;

/// <summary>
///     Defines a store that notifies when a message arrives on a message queue
/// </summary>
public interface IQueueStoreTrigger
{
    event MessageQueueUpdated FireQueueMessage;
}

/// <summary>
///     Defines a delegate for handling when messages arrive on a message queue
/// </summary>
public delegate void MessageQueueUpdated(object sender, MessagesQueueUpdatedArgs args);

/// <summary>
///     Defines the arguments for when messages arrive on a message queue
/// </summary>
public class MessagesQueueUpdatedArgs
{
    public MessagesQueueUpdatedArgs(string queueName, int messageCount)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName));
        QueueName = queueName;
        MessageCount = messageCount;
    }

    public int MessageCount { get; }

    public string QueueName { get; }
}

#endif