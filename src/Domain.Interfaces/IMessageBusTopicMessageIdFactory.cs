namespace Domain.Interfaces;

/// <summary>
///     Defines a factory for creating and validating message identifiers for message bus topics
/// </summary>
public interface IMessageBusTopicMessageIdFactory
{
    string Create(string topicName);

    bool IsValid(string id);
}