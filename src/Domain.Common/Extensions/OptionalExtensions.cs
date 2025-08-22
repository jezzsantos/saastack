using Common;

namespace Domain.Common.Extensions;

public static class OptionalExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="value" /> from an <see cref="Optional{String}" />
    ///     to a <see cref="Nullable{TValue}" /> using the specified converter
    /// </summary>
    public static TValue? ToNullable<TValue>(this Optional<string> value, Func<Optional<string>, TValue?> converter,
        // ReSharper disable once UnusedParameter.Global
        ValueTypeTag<TValue>? tag = null)
        where TValue : struct
    {
        return value.HasValue
            ? converter(value.Value)
            : null;
    }

    /// <summary>
    ///     Converts the specified <see cref="value" /> from an <see cref="Optional{String}" />
    ///     to a <see cref="Nullable{TValue}" /> using the specified converter
    /// </summary>
    public static TValue? ToNullable<TValue>(this Optional<string> value, Func<Optional<string>, TValue?> converter,
        // ReSharper disable once UnusedParameter.Global
        ReferenceTypeTag<TValue>? tag = null)
        where TValue : class
    {
        return value.HasValue
            ? converter(value.Value)
            : default;
    }

    /// <summary>
    ///     Converts the specified <see cref="value" /> from an <see cref="Optional{String}" />
    ///     to a <see cref="Optional{TValue}" /> using the specified converter
    /// </summary>
    public static Optional<TValue> ToOptional<TValue>(this Optional<string> value, Func<string, TValue> converter)
    {
        return value.HasValue
            ? converter(value.Value)
            : Optional<TValue>.None;
    }
}