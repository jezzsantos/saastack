using Common.Extensions;

namespace Infrastructure.Persistence.Azure.Extensions;

public static class ValidationExtensions
{
    public const string StorageAccountResourceNameValidationExpression = @"^[a-z0-9\-]{3,63}$";

    public static string SanitiseAndValidateStorageAccountResourceName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateStorageAccountResourceName(lowercased);

        return lowercased;
    }

    private static void ValidateStorageAccountResourceName(string name)
    {
        if (!name.IsMatchWith(StorageAccountResourceNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidStorageAccountResourceName.Format(name));
        }
    }
}