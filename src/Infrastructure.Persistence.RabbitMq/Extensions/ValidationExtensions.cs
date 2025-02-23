using Common.Extensions;

namespace Infrastructure.Persistence.RabbitMq.Extensions
{
    public static class ValidationExtensions
    {
        public static string SanitizeAndValidateStorageAccountResourceName(this string name)
        {
            var lowercased = name.ToLowerInvariant();
            ValidateStorageAccountResourceName(lowercased);
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

        private static void ValidateStorageAccountResourceName(string name)
        {
            if (!name.IsMatchWith(RabbitMqConstants.StorageAccountResourceNameValidationExpression))
            {
                throw new ArgumentOutOfRangeException(
                    Resources.ValidationExtensions_InvalidStorageAccountResourceName.Format(name));
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
}
