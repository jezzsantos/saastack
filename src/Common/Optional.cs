using System.Diagnostics;
using System.Reflection;
using Common.Extensions;
using JetBrains.Annotations;

namespace Common;

/// <summary>
///     Provides shortcuts for <see cref="Optional{T}" />
/// </summary>
[DebuggerStepThrough]
public static class Optional
{
    /// <summary>
    ///     Changes the type of the specified <see cref="optional" /> to an <see cref="Optional" />
    ///     of the specified <see cref="targetType" />
    /// </summary>
    public static object ChangeType(Optional<object> optional, Type targetType)
    {
        optional.TryGetContainedValue(out var containedValue);

        return ChangeOptionalType(containedValue, targetType);
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is of type <see cref="Optional{T}" />, and if so returns the contained value
    /// </summary>
    public static bool IsOptional(this object? value, out object? contained)
    {
        return value.TryGetContainedValue(out contained);
    }

    /// <summary>
    ///     Whether the <see cref="type" /> is an optional type
    /// </summary>
    public static bool IsOptionalType(Type type)
    {
        return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);
    }

    /// <summary>
    ///     Whether the <see cref="type" /> is an optional type, and if so return the <see cref="containedType" />
    /// </summary>
    public static bool IsOptionalType(Type type, out Type? containedType)
    {
        var isOptional = IsOptionalType(type);
        if (!isOptional)
        {
            containedType = null;
            return false;
        }

        containedType = containedType = type.GetGenericArguments()[0];
        return true;
    }

    /// <summary>
    ///     Returns an <see cref="Optional{TValue}" /> with no value
    /// </summary>
    public static Optional<TValue> None<TValue>()
    {
        return Optional<TValue>.None;
    }

    /// <summary>
    ///     Returns an <see cref="Optional{TValue}" /> with a value
    /// </summary>
    public static Optional<TValue> Some<TValue>(TValue value)
    {
        value.ThrowIfNullParameter(nameof(value));

        if (value.TryGetContainedValue(out var contained))
        {
            if (contained.NotExists())
            {
                return Optional<TValue>.None;
            }

            if (contained.GetType() == typeof(TValue))
            {
                return value;
            }

            // ReSharper disable once TailRecursiveCall
            return Some<TValue>((TValue)contained);
        }

        return new Optional<TValue>(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="optional" /> to an <see cref="TIn" />
    /// </summary>
    // ReSharper disable once UnusedParameter.Global
    public static TIn? ToNullable<TIn>(this Optional<TIn> optional, ValueTypeTag<TIn>? tag = null)
        where TIn : struct
    {
        return optional.ToNullable<TIn, TIn>();
    }

    /// <summary>
    ///     Converts the specified <see cref="optional" /> to an <see cref="TIn" />
    /// </summary>
    // ReSharper disable once UnusedParameter.Global
    public static TIn? ToNullable<TIn>(this Optional<TIn?> optional, ValueTypeTag<TIn>? tag = null)
        where TIn : struct
    {
        return optional.ToNullable<TIn?, TIn>();
    }

    /// <summary>
    ///     Converts the specified <see cref="optional" /> to an <see cref="TIn" />
    /// </summary>
    // ReSharper disable once UnusedParameter.Global
    public static TIn? ToNullable<TIn>(this Optional<TIn> optional, ReferenceTypeTag<TIn>? tag = null)
        where TIn : class?
    {
        return optional.ToNullable<TIn, TIn>();
    }

    /// <summary>
    ///     Converts the specified <see cref="optional" /> to an <see cref="TOut" />
    /// </summary>
    public static TOut? ToNullable<TIn, TOut>(this Optional<TIn> optional, Func<TIn, TOut>? converter = null,
        // ReSharper disable once UnusedParameter.Global
        ReferenceTypeTag<TOut>? tag = null)
        where TOut : class?
    {
        if (converter.Exists())
        {
            return optional.HasValue
                ? converter(optional.Value)
                : default;
        }

        if (!optional.HasValue)
        {
            return default;
        }

        var changed = Convert.ChangeType(optional.Value, typeof(TOut));
        if (changed.NotExists())
        {
            return default;
        }

        return (TOut)changed;
    }

    /// <summary>
    ///     Converts the specified <see cref="optional" /> to an <see cref="TOut" />
    /// </summary>
    public static TOut? ToNullable<TIn, TOut>(this Optional<TIn> optional, Func<TIn, TOut?>? converter = null,
        // ReSharper disable once UnusedParameter.Global
        ValueTypeTag<TOut>? tag = null)
        where TOut : struct
    {
        if (converter.Exists())
        {
            return optional.HasValue
                ? converter(optional.Value)
                : null;
        }

        if (!optional.HasValue)
        {
            return null;
        }

        var changed = Convert.ChangeType(optional.Value, typeof(TOut));
        if (changed.NotExists())
        {
            return null;
        }

        return (TOut)changed;
    }

    /// <summary>
    ///     Converts the specified <see cref="value" /> to an <see cref="Optional{TOut}" />
    ///     using the optional <see cref="converter" /> to convert the value (if any)
    /// </summary>
    public static Optional<TOut> ToOptional<TIn, TOut>(this TIn? value, Func<TIn, TOut> converter,
        // ReSharper disable once UnusedParameter.Global
        ReferenceTypeTag<TOut>? tag = null)
        where TOut : class?
    {
        if (value.NotExists())
        {
            return Optional<TOut>.None;
        }

        if (converter.Exists())
        {
            return converter(value).ToOptional(tag);
        }

        var changed = Convert.ChangeType(value, typeof(TOut));
        if (changed.NotExists())
        {
            return Optional<TOut>.None;
        }

        return ((TOut)changed).ToOptional(tag);
    }

    /// <summary>
    ///     Converts the specified <see cref="value" /> to an <see cref="Optional{TOut}" />
    ///     using the optional <see cref="converter" /> to convert the value (if any)
    /// </summary>
    public static Optional<TOut> ToOptional<TIn, TOut>(this TIn? value, Func<TIn, TOut?> converter,
        // ReSharper disable once UnusedParameter.Global
        ValueTypeTag<TOut>? tag = null)
        where TOut : struct
    {
        if (value.NotExists())
        {
            return Optional<TOut>.None;
        }

        if (converter.Exists())
        {
            var converted = converter(value);
            return converted.ToOptional(tag);
        }

        var changed = Convert.ChangeType(value, typeof(TOut));
        if (changed.NotExists())
        {
            return Optional<TOut>.None;
        }

        return ((TOut?)changed).ToOptional(tag);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an <see cref="Optional{TValue}" />
    /// </summary>
    public static Optional<TValue> ToOptional<TValue>(this TValue? value,
        // ReSharper disable once UnusedParameter.Global
        ReferenceTypeTag<TValue>? tag = null)
        where TValue : class?
    {
        return value.Exists()
            ? Some<TValue>(value)
            : None<TValue>();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an <see cref="Optional" /> of the specified <see cref="targetType" />
    /// </summary>
    public static object ToOptional<TValue>(this TValue? value, Type targetType)
    {
        return ChangeOptionalType(value, targetType);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an <see cref="Optional{TValue}" />
    /// </summary>
    public static Optional<TValue> ToOptional<TValue>(this TValue? value,
        // ReSharper disable once UnusedParameter.Global
        ValueTypeTag<TValue>? tag = null)
        where TValue : struct
    {
        return value.HasValue
            ? Some(value.Value)
            : None<TValue>();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an <see cref="Optional{TValue}" />
    /// </summary>
    public static Optional<TValue> ToOptional<TValue>(this TValue value,
        // ReSharper disable once UnusedParameter.Global
        ValueTypeTag<TValue>? tag = null)
        where TValue : struct
    {
        return Some(value);
    }

    /// <summary>
    ///     Returns the type of the contained optional type.
    ///     For example this would return <see cref="TValue" /> if <see cref="type" /> was <see cref="Optional{TValue}" />
    /// </summary>
    public static bool TryGetContainedType(Type type, out Type? containedType)
    {
        containedType = null;
        return IsOptionalType(type, out containedType);
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is of type <see cref="Optional{T}" />, and if so returns the contained value
    /// </summary>
    public static bool TryGetContainedValue(this object? value, out object? contained)
    {
        contained = null;
        if (value.NotExists())
        {
            return false;
        }

        var typeOfOldOptional = value.GetType();
        if (!IsOptionalType(typeOfOldOptional))
        {
            return false;
        }

        var typeOfContainedValue = typeOfOldOptional.GenericTypeArguments[0];
        var typeOfNewOptional = typeof(Optional<>).MakeGenericType(typeOfContainedValue);
        var valueOrDefault = typeOfNewOptional.InvokeMember(nameof(Optional<object>.ValueOrDefault),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, value, null);

        contained = valueOrDefault;
        return true;
    }

    private static object ChangeOptionalType(object? value, Type targetType)
    {
        var targetOptionalType = typeof(Optional<>).MakeGenericType(targetType);
        var ctor = targetOptionalType.GetConstructor([targetType]);
        var instance = ctor!.Invoke([value]);
        return instance;
    }
}

/// <summary>
///     Provides an optional type that combines a <see cref="Value" /> and a <see cref="HasValue" /> which indicates
///     whether the <see cref="Value" /> is meaningful.
/// </summary>
[DebuggerStepThrough]
public readonly struct Optional<TValue> : IEquatable<Optional<TValue>>
{
    public const string NoValueStringValue = nameof(None);
    private const string NullValueStringValue = "null";

    /// <summary>
    ///     Returns the container without any value
    /// </summary>
    public static Optional<TValue> None => new();

    /// <summary>
    ///     Returns the container with a value
    /// </summary>
    public static Optional<TValue> Some(TValue? some)
    {
        return new Optional<TValue>(some);
    }

    public Optional(TValue? value)
    {
        ValueOrDefault = value;
        ValueOrNull = value.NotExists()
            ? null
            : value;
        HasValue = value.Exists();
    }

    public Optional(Optional<TValue> value)
    {
        ValueOrDefault = value.ValueOrDefault;
        ValueOrNull = value;
        HasValue = value.HasValue;
    }

    public Optional(Optional<Optional<TValue>> value)
    {
        ValueOrDefault = value.ValueOrDefault.ValueOrDefault;
        ValueOrNull = value.ValueOrNull;
        HasValue = value.ValueOrDefault.HasValue;
    }

    /// <summary>
    ///     Whether there is a value
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    ///     Returns the contained value
    /// </summary>
    public TValue Value
    {
        [DebuggerStepThrough]
        get
        {
            if (!HasValue)
            {
                throw new InvalidOperationException(Resources.Optional_NullValue);
            }

            return ValueOrDefault!;
        }
    }

    /// <summary>
    ///     Returns the contained value
    /// </summary>
    public TValue? ValueOrDefault { get; }

    /// <summary>
    ///     Returns either the contained value, or null
    /// </summary>
    public object? ValueOrNull { get; }

    /// <summary>
    ///     Returns either the contained value, or null
    /// </summary>
    public object? ToValueOrNull(Func<TValue, object?> converter)
    {
        return HasValue
            ? converter(Value)
            : ValueOrNull;
    }

    /// <summary>
    ///     Tries to obtain the value
    /// </summary>
    public bool TryGet(out TValue value)
    {
        value = ValueOrDefault!;
        return HasValue;
    }

    /// <summary>
    ///     Casts the specified <see cref="value" /> to an optional of the same type
    /// </summary>
    [DebuggerStepThrough]
    public static implicit operator Optional<TValue>(TValue? value)
    {
        return value.Exists()
            ? new Optional<TValue>(value)
            : None;
    }

    /// <summary>
    ///     Casts the specified optional <see cref="value" /> to its contained value
    /// </summary>
    [DebuggerStepThrough]
    public static implicit operator TValue(Optional<TValue> value)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return value.HasValue
            ? value.ValueOrDefault
            : default;
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    ///     Returns a string representation of this value
    /// </summary>
    public override string ToString()
    {
        return HasValue
            ? Value?.ToString() ?? NullValueStringValue
            : NoValueStringValue;
    }

    /// <summary>
    ///     Whether this instance contains the same value as the contained value from the <see cref="other" />
    /// </summary>
    public bool Equals(Optional<TValue> other)
    {
        if (!HasValue)
        {
            return !other.HasValue;
        }

        if (other.HasValue)
        {
            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        return false;
    }

    /// <summary>
    ///     Whether this instance contains the same value as the <see cref="other" /> instance
    /// </summary>
    public override bool Equals(object? other)
    {
        if (other is Optional<TValue> optional)
        {
            return Equals(optional);
        }

        if (other is TValue instance)
        {
            return Equals(instance);
        }

        return false;
    }

    /// <summary>
    ///     Gets the unique hashed value of the contained value
    /// </summary>
    public override int GetHashCode()
    {
        return HasValue
            ? EqualityComparer<TValue>.Default.GetHashCode(Value!)
            : 0;
    }

    /// <summary>
    ///     Whether the <see cref="left" /> contains the same value as the contained value from the <see cref="right" />
    /// </summary>
    public static bool operator ==(Optional<TValue> left, Optional<TValue> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> contains a different value from the contained value of the <see cref="right" />
    /// </summary>
    public static bool operator !=(Optional<TValue> left, Optional<TValue> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> <see cref="Optional{T}" /> contains the same value as the <see cref="right" />
    ///     instance
    /// </summary>
    public static bool operator ==(Optional<TValue> left, TValue right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> <see cref="Optional{T}" /> contains a different value from the <see cref="right" />
    ///     instance
    /// </summary>
    public static bool operator !=(Optional<TValue> left, TValue right)
    {
        if (right.NotExists())
        {
            return left.Exists();
        }

        return !(left == right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> instance is the same value as the contained value of the <see cref="right" />
    /// </summary>
    public static bool operator ==(TValue left, Optional<TValue> right)
    {
        return right.Equals(left);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> instance is different from the contained value of the <see cref="right" />
    /// </summary>
    public static bool operator !=(TValue left, Optional<TValue> right)
    {
        if (left.NotExists())
        {
            return right.Exists();
        }

        return !(right == left);
    }
}

/// <summary>
///     Defines a marker type for value types
/// </summary>
// ReSharper disable once UnusedTypeParameter
[UsedImplicitly]
public sealed class ValueTypeTag<T>
    where T : struct
{
}

/// <summary>
///     Defines a marker type for reference types
/// </summary>
// ReSharper disable once UnusedTypeParameter
[UsedImplicitly]
public sealed class ReferenceTypeTag<T>
    where T : class?
{
}