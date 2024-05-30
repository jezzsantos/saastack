namespace Infrastructure.Persistence.Azure;

public static class AzureConstants
{
    public const string ServiceBusSubscriptionNameValidationExpression = @"^[a-z0-9_\.\-]{1,50}$";
    public const string ServiceBusTopicNameValidationExpression = @"^[a-z0-9_\.\-\/]{1,260}$";
    public const string StorageAccountResourceNameValidationExpression = @"^[a-z0-9\-]{3,63}$";
}