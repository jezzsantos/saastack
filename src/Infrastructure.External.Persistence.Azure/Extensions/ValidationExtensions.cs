using Common.Extensions;

namespace Infrastructure.External.Persistence.Azure.Extensions;

public static class ValidationExtensions
{
    /// <summary>
    ///     Sanitizes and validates the storage account resource name.
    ///     Limitations here:
    ///     <see
    ///         href="https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#container-names" />
    /// </summary>
    public static string SanitizeAndValidateStorageAccountResourceName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateStorageAccountResourceName(lowercased);

        return lowercased;
    }

    /// <summary>
    ///     Sanitizes and validates the service bus subscription name.
    ///     Limitations here:
    ///     <see
    ///         href="https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules#microsoftservicebus" />
    /// </summary>
    public static string SanitizeAndValidateSubscriptionName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateServiceBusSubscriptionName(lowercased);

        return lowercased;
    }

    /// <summary>
    ///     Sanitizes and validates the service bus topic name.
    ///     Limitations here:
    ///     <see
    ///         href="https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules#microsoftservicebus" />
    /// </summary>
    public static string SanitizeAndValidateTopicName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateServiceBusTopicName(lowercased);

        return lowercased;
    }

    private static void ValidateStorageAccountResourceName(string name)
    {
        if (!name.IsMatchWith(AzureConstants.StorageAccountResourceNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidStorageAccountResourceName.Format(name));
        }
    }

    private static void ValidateServiceBusTopicName(string name)
    {
        if (!name.IsMatchWith(AzureConstants.ServiceBusTopicNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusTopicName.Format(name));
        }
    }

    private static void ValidateServiceBusSubscriptionName(string name)
    {
        if (!name.IsMatchWith(AzureConstants.ServiceBusSubscriptionNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusSubscriptionName.Format(name));
        }
    }
}