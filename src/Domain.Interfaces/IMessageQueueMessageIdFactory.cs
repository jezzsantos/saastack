namespace Domain.Interfaces;

/// <summary>
///     Defines a factory for creating and validating message identifiers for queued messages
/// </summary>
public interface IMessageQueueMessageIdFactory
{
    string Create(string queueName);

    bool IsValid(string id);
}