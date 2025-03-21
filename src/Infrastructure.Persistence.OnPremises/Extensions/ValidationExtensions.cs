using Common.Extensions;

namespace Infrastructure.Persistence.OnPremises.Extensions;

public static class ValidationExtensions
{
    public static string SanitizeAndValidateInvalidDatabaseResourceName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateInvalidDatabaseResourceName(lowercased);
        return lowercased;
    }

    public static string SanitizeAndValidateSubscriptionName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateRabbitMqQueueName(lowercased);
        return lowercased;
    }

    public static string SanitizeAndValidateTopicName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateRabbitMqExchangeName(lowercased);
        return lowercased;
    }

    private static void ValidateInvalidDatabaseResourceName(string name)
    {
        if (!name.IsMatchWith(RabbitMqConstants.DatabaseResourceNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidDatabaseResourceName.Format(name));
        }
    }

    private static void ValidateRabbitMqExchangeName(string name)
    {
        if (!name.IsMatchWith(RabbitMqConstants.RabbitMqExchangeNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusTopicName.Format(name));
        }
    }

    private static void ValidateRabbitMqQueueName(string name)
    {
        if (!name.IsMatchWith(RabbitMqConstants.RabbitMqQueueNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusSubscriptionName.Format(name));
        }
    }
}