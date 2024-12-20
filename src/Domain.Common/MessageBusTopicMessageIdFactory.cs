using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Validations;

namespace Domain.Common;

/// <summary>
///     Provides a factory for creating identifiers for message bus topic messages
/// </summary>
public class MessageBusTopicMessageIdFactory : IMessageBusTopicMessageIdFactory
{
    public static readonly int MaxTopicName = CommonValidations.Messaging.Ids.MaxPrefixLength;

    public string Create(string topicName)
    {
        topicName.ThrowIfInvalidParameter(CommonValidations.Messaging.Ids.TopicName, nameof(topicName),
            Resources.MessageBusTopicMessageIdFactory_InvalidTopicName);

        return $"{topicName.ToLowerInvariant()}_{Guid.NewGuid():N}";
    }

    public bool IsValid(string id)
    {
        id.ThrowIfNotValuedParameter(nameof(id));

        return CommonValidations.Messaging.Ids.MessageId.Matches(id);
    }
}