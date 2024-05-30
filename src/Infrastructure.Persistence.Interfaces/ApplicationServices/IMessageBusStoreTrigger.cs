#if TESTINGONLY
namespace Infrastructure.Persistence.Interfaces.ApplicationServices;

/// <summary>
///     Defines a store that notifies when a message arrives on a message bus topic
/// </summary>
public interface IMessageBusStoreTrigger
{
    event MessageQueueUpdated FireTopicMessage;
}

#endif