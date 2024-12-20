using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Validations;

namespace Domain.Common;

/// <summary>
///     Provides a factory for creating identifiers for queued messages
/// </summary>
public class MessageQueueMessageIdFactory : IMessageQueueMessageIdFactory
{
    public static readonly int MaxQueueName = CommonValidations.Messaging.Ids.MaxPrefixLength;

    public string Create(string queueName)
    {
        queueName.ThrowIfInvalidParameter(CommonValidations.Messaging.Ids.QueueName, nameof(queueName),
            Resources.MessageQueueMessageIdFactory_InvalidQueueName);

        return $"{queueName.ToLowerInvariant()}_{Guid.NewGuid():N}";
    }

    public bool IsValid(string id)
    {
        id.ThrowIfNotValuedParameter(nameof(id));

        return CommonValidations.Messaging.Ids.MessageId.Matches(id);
    }
}