#if COMMON_PROJECT
using AutoMapper;
#endif
using System.Diagnostics;
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM || ANALYZERS_PLATFORM
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
#endif

namespace Common.Extensions;

[DebuggerStepThrough]
public static class ObjectExtensions
{
#if COMMON_PROJECT
    /// <summary>
    ///     Auto-maps the <see cref="source" /> to a new instance of the <see cref="TTarget" /> instance
    /// </summary>
    public static TTarget Convert<TSource, TTarget>(this TSource source)
    {
        var configuration = new MapperConfiguration(cfg => cfg.CreateMap<TSource, TTarget>());
        var mapper = configuration.CreateMapper();

        return mapper.Map<TTarget>(source);
    }
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM || ANALYZERS_PLATFORM
    /// <summary>
    ///     Whether the object does exist
    /// </summary>
    [ContractAnnotation("null => false; notnull => true")]
    public static bool Exists([NotNullWhen(true)] this object? instance)
    {
        return instance is not null;
    }
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM || ANALYZERS_PLATFORM
    /// <summary>
    ///     Whether the object does not exist
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    public static bool NotExists([NotNullWhen(false)] this object? instance)
    {
        return instance is null;
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Whether the parameter <see cref="value" /> from being invalid according to the <see cref="validation" />,
    ///     and if invalid, returns a <see cref="ErrorCode.Validation" /> error
    /// </summary>
    public static bool IsInvalidParameter<TValue>(this TValue? value, Predicate<TValue> predicate,
        string parameterName, string? errorMessage, out Error error)
    {
        if (value.NotExists())
        {
            error = errorMessage.HasValue()
                ? Error.Validation(errorMessage)
                : Error.Validation(parameterName);
            return true;
        }

        return IsInvalidParameter(() => predicate(value), parameterName, errorMessage, out error);
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> from being invalid according to the <see cref="validation" />,
    ///     and if invalid, returns a <see cref="ErrorCode.Validation" /> error
    /// </summary>
    public static bool IsInvalidParameter<TValue>(this TValue? value, Predicate<TValue> predicate,
        string parameterName, out Error error)
    {
        if (value.NotExists())
        {
            error = Error.Validation(parameterName);
            return true;
        }

        return IsInvalidParameter(() => predicate(value), parameterName, null, out error);
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> has any value,
    ///     and if invalid, returns a <see cref="ErrorCode.Validation" /> error
    /// </summary>
    public static bool IsNotValuedParameter(this string? value, string parameterName, string? errorMessage,
        out Error error)
    {
        return IsInvalidParameter(value.HasValue, parameterName, errorMessage, out error);
    }

    /// <summary>
    ///     Whether the parameter <see cref="value" /> has any value,
    ///     and if invalid, returns a <see cref="ErrorCode.Validation" /> error
    /// </summary>
    public static bool IsNotValuedParameter(this string? value, string parameterName, out Error error)
    {
        return IsInvalidParameter(value.HasValue, parameterName, null, out error);
    }
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Populates the public properties of the <see cref="target" /> instance with the values of matching public properties
    ///     of the
    ///     <see cref="source" /> instance, whether those values have default or non-default values.
    /// </summary>
    public static void PopulateWith<TType>(this TType target, TType source)
    {
        target.PopulateWith(source.ToObjectDictionary());
    }

    /// <summary>
    ///     Populates the public properties of the <see cref="target" /> instance with the values of matching properties of the
    ///     <see cref="source" /> instance, whether those values have default or non-default values.
    /// </summary>
    public static void PopulateWith<TType>(this TType target, IReadOnlyDictionary<string, object?> source)
    {
        var configuration = new MapperConfiguration(_ => { });
        var mapper = configuration.CreateMapper();

        mapper.Map(source, target);
    }

    /// <summary>
    ///     Throws an <see cref="ArgumentOutOfRangeException" /> if the specified <see cref="value" /> is invalid
    /// </summary>
    public static void ThrowIfInvalidParameter<TValue>(this TValue? value, Predicate<TValue?> predicate,
        string parameterName, string? errorMessage = null)
    {
        if (value.IsInvalidParameter(predicate, parameterName, errorMessage, out _))
        {
            throw new ArgumentOutOfRangeException(parameterName, errorMessage);
        }
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Throws an <see cref="ArgumentOutOfRangeException" /> if the specified <see cref="value" /> does not have a value
    /// </summary>
    public static void ThrowIfNotValuedParameter(this string? value, string parameterName, string? errorMessage = null)
    {
        if (value.IsNotValuedParameter(parameterName, errorMessage, out _))
        {
            throw new ArgumentOutOfRangeException(parameterName, errorMessage);
        }
    }

    private static bool IsInvalidParameter(Func<bool> predicate, string parameterName, string? errorMessage,
        out Error error)
    {
        var isValid = predicate();
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

    /// <summary>
    ///     Throws an <see cref="ArgumentNullException" /> if the specified <see cref="value" /> is null
    /// </summary>
    public static void ThrowIfNullParameter<TValue>(this TValue? value, string parameterName,
        string? errorMessage = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName, errorMessage);
        }
    }
#endif
}