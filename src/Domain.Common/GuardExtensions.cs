using Common.Extensions;
using Domain.Interfaces.Validations;

namespace Domain.Common;

public static class GuardExtensions
{
    /// <summary>
    ///     Guards the <see cref="value" /> from being invalid according to the <see cref="validation" />
    /// </summary>
    /// <exception cref="ArgumentException">When the <see cref="parameterName" /> is missing</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     When the <see cref="value" /> fails the <see cref="validation" />
    /// </exception>
    public static void GuardAgainstInvalid<TValue>(this TValue value, Validation<TValue> validation,
        string parameterName, string? errorMessage = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(parameterName);

        var isMatch = validation.Matches(value);
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