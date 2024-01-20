namespace Domain.Interfaces;

/// <summary>
///     Defines a factory for creating and validating identifiers for queued messages
/// </summary>
public interface IMessageQueueIdFactory
{
    string Create(string queueName);

    bool IsValid(string id);
}