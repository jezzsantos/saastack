using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Validations;

namespace Domain.Common;

/// <summary>
///     Provides a factory for creating identifiers for queued messages
/// </summary>
public class MessageQueueIdFactory : IMessageQueueIdFactory
{
    public static readonly int MaxQueueName = CommonValidations.MessageQueues.Ids.MaxPrefixLength;

    public string Create(string queueName)
    {
        queueName.ThrowIfInvalidParameter(CommonValidations.MessageQueues.Ids.Prefix, nameof(queueName),
            Resources.MessageQueueFactory_InvalidQueueName);

        return $"{queueName.ToLowerInvariant()}_{Guid.NewGuid():N}";
    }

    public bool IsValid(string id)
    {
        id.ThrowIfNotValuedParameter(nameof(id));

        return CommonValidations.MessageQueues.Ids.Id.Matches(id);
    }
}