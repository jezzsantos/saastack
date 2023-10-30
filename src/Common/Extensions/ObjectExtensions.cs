using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Common.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    ///     Whether the object does exist
    /// </summary>
    [ContractAnnotation("null => false; notnull => true")]
    public static bool Exists([NotNullWhen(true)] this object? instance)
    {
        return instance is not null;
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> from being invalid according to the <see cref="validation" />,
    ///     and if invalid, returns a <see cref="PreconditionViolation" />
    /// </summary>
    public static bool IsInvalidParameter<TValue>(this TValue value, Func<TValue, bool> validator,
        string parameterName, string? errorMessage, out Error error)
    {
        return IsInvalidParameter(() => validator(value), parameterName, errorMessage, out error);
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> from being invalid according to the <see cref="validation" />,
    ///     and if invalid, returns a <see cref="PreconditionViolation" />
    /// </summary>
    public static bool IsInvalidParameter<TValue>(this TValue value, Func<TValue, bool> validator,
        string parameterName, out Error error)
    {
        return IsInvalidParameter(() => validator(value), parameterName, null, out error);
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> has any value,
    ///     and if invalid, returns a <see cref="PreconditionViolation" />
    /// </summary>
    public static bool IsNotValuedParameter(this string? value, string parameterName, string? errorMessage,
        out Error error)
    {
        return IsInvalidParameter(value.HasValue, parameterName, errorMessage, out error);
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> has any value,
    ///     and if invalid, returns a <see cref="PreconditionViolation" />
    /// </summary>
    public static bool IsNotValuedParameter(this string? value, string parameterName, out Error error)
    {
        return IsInvalidParameter(value.HasValue, parameterName, null, out error);
    }

    /// <summary>
    ///     Whether the object does not exist
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    public static bool NotExists([NotNullWhen(false)] this object? instance)
    {
        return instance is null;
    }

    private static bool IsInvalidParameter(Func<bool> validationFunc, string parameterName, string? errorMessage,
        out Error error)
    {
        var isValid = validationFunc();
        if (!isValid)
        {
            error = errorMessage.HasValue()
                ? Error.Validation(errorMessage)
                : Error.Validation(parameterName);
            return true;
        }

        error = Error.NoError;
        return false;
    }
}