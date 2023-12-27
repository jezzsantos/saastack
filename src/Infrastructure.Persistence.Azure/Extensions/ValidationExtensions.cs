using Common.Extensions;

namespace Infrastructure.Persistence.Azure.Extensions;

public static class ValidationExtensions
{
    public static string SanitizeAndValidateStorageAccountResourceName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateStorageAccountResourceName(lowercased);

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
}