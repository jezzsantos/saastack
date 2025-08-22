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
        if (optional.TryGetOptionalValue(out var descriptor))
        {
            return ChangeOptionalType(descriptor!.ContainedValue, targetType);
        }

        return ChangeOptionalType(null, targetType);
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is of type <see cref="Optional{T}" />, and if so returns the contained value
    /// </summary>
    public static bool IsOptional(this object? value, out OptionalDescriptor? descriptor)
    {
        return value.TryGetOptionalValue(out descriptor);
    }

    /// <summary>
    ///     Whether the <see cref="type" /> is an optional type
    /// </summary>
    public static bool IsOptionalType(Type type)
    {
        return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);
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
    public static Optional<TValue> Some<TValue>(TValue? value)
    {
        return value.Exists()
            ? Optional<TValue>.Some(value)
            : Optional<TValue>.None;
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
        return Some(value);
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
    ///     Whether the <see cref="type" /> is of type <see cref="Optional{T}" />,
    ///     and if so, returns the optional type.
    /// </summary>
    [ContractAnnotation("=> true, containedType: notnull; => false, containedType: null")]
    public static bool TryGetOptionalType(Type type, out Type? containedType)
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
    ///     Whether the <see cref="value" /> is of type <see cref="Optional{T}" />,
    ///     and if so, returns the optional type and value
    /// </summary>
    [ContractAnnotation("=> true, descriptor: notnull; => false, descriptor: null")]
    public static bool TryGetOptionalValue(this object? value, out OptionalDescriptor? descriptor)
    {
        descriptor = null;
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
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, value, null)!;
        var hasValue = (bool)typeOfNewOptional.InvokeMember(nameof(Optional<object>.HasValue),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, value, null)!;

        descriptor = new OptionalDescriptor(typeOfContainedValue, valueOrDefault, !hasValue);
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
///     A descriptor for an optional value
/// </summary>
public class OptionalDescriptor
{
    public OptionalDescriptor(Type containedType, object containedValue, bool isNone)
    {
        ContainedType = containedType;
        ContainedValue = containedValue;
        IsNone = isNone;
    }

    public Type ContainedType { get; }

    public object ContainedValue { get; }

    public bool IsNone { get; }
}

/// <summary>
///     Provides an optional type that combines a <see cref="Value" /> and a <see cref="HasValue" /> which indicates
///     whether the <see cref="Value" /> is meaningful.
///     Nesting of a <see cref="Optional{TValue}" /> value within another <see cref="Optional{TValue}" />,
///     is not permitted, and if done statically, will simply wrap the inner value.
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
    public static Optional<TValue> Some(TValue? value)
    {
        if (value.IsOptional(out _))
        {
            throw new ArgumentOutOfRangeException(nameof(value), Resources.Optional_WrappingOptional);
        }

        return new Optional<TValue>(value);
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
    [ContractAnnotation("=> true, value: notnull; => false, value: null")]
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