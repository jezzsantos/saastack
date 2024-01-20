using Common;
using Common.Extensions;
using Domain.Interfaces.Validations;

namespace Domain.Common.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    ///     Guards the parameter <see cref="value" /> from being invalid according to the <see cref="validation" />,
    ///     and if invalid, returns a <see cref="PreconditionViolation" />
    /// </summary>
    public static bool IsInvalidParameter<TValue>(this TValue value, Validation<TValue> validation,
        string parameterName, string? errorMessage, out Error error)
    {
        var isValid = validation.Matches(value);
        if (!isValid)
        {
            error = errorMessage.HasValue()
                ? Error.Validation(errorMessage)
                : Error.Validation(parameterName);
            return true;
        }

        error = default;
        return false;
    }

    /// <summary>
    ///     Guards the parameter <see cref="value" /> from being invalid according to the <see cref="validation" />,
    ///     and if invalid, returns a <see cref="PreconditionViolation" />
    /// </summary>
    public static void ThrowIfInvalidParameter<TValue>(this TValue value, Validation<TValue> validation,
        string parameterName, string? errorMessage)
    {
        value.ThrowIfInvalidParameter(validation!.Matches, parameterName, errorMessage);
    }
}