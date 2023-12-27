using Common.Extensions;

namespace Infrastructure.Persistence.AWS.Extensions;

public static class ValidationExtensions
{
    public static string SanitizeAndValidateQueueName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateQueueName(lowercased);

        return lowercased;
    }

    private static void ValidateQueueName(string name)
    {
        if (!name.IsMatchWith(AWSConstants.SqsQueueNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidQueueName.Format(name));
        }
    }
}