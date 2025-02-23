namespace Infrastructure.Persistence.RabbitMq;

public static class RabbitMqConstants
{
    public const string RabbitMqQueueNameValidationExpression = @"^[a-z0-9_\.\-]{1,255}$";

    public const string RabbitMqExchangeNameValidationExpression = @"^[a-z0-9_\.\-\/]{1,255}$";

    public const string StorageAccountResourceNameValidationExpression = @"^[a-z0-9\-]{3,63}$";
}
