using Common.Extensions;
using Domain.Interfaces.Validations;

namespace Domain.Common;

public static class GuardExtensions
{
    public static void GuardAgainstInvalid<TValue>(this TValue value, Validation<TValue> format, string parameterName,
        string? errorMessage = null) where TValue : notnull
    {
        ArgumentException.ThrowIfNullOrEmpty(parameterName);

        var isMatch = format.Matches(value);
        if (!isMatch)
        {
            if (errorMessage.HasValue())
            {
                throw new ArgumentOutOfRangeException(parameterName, errorMessage);
            }

            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}