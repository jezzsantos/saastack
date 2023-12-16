using System.Diagnostics;
using System.Reflection;
using Common.Extensions;

namespace Common;

/// <summary>
///     Provides shortcuts for <see cref="Optional{T}" />
/// </summary>
public static class Optional
{
    /// <summary>
    ///     Whether the <see cref="value" /> is of type <see cref="Optional{T}" />, and if so returns the contained value
    /// </summary>
    public static bool IsOptional(this object? value, out object? contained)
    {
        contained = default;
        if (value is null)
        {
            return false;
        }

        var typeOfValue = value.GetType();
        if (IsOptionalType(typeOfValue))
        {
            var containedType = typeof(Optional<>).MakeGenericType(typeOfValue.GenericTypeArguments[0]);
            var valueOrDefault = containedType.InvokeMember(nameof(Optional<object>.ValueOrDefault),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, value, null);

            contained = valueOrDefault;
            return true;
        }

        return false;
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
    public static Optional<TValue> Some<TValue>(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.IsOptional(out var contained))
        {
            if (contained is null)
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
    ///     Converts the <see cref="value" /> to an <see cref="Optional{TValue}" />
    /// </summary>
    public static Optional<TValue> ToOptional<TValue>(this TValue? value)
    {
        return value is null
            ? None<TValue>()
            : Some<TValue>(value);
    }

    /// <summary>
    ///     Returns the type of the contained optional type.
    ///     For example this would return <see cref="TValue" /> if <see cref="type" /> was <see cref="Optional{TValue}" />
    /// </summary>
    public static bool TryGetContainedType(Type type, out Type? containedType)
    {
        containedType = null;
        if (!IsOptionalType(type))
        {
            return false;
        }

        containedType = type.GetGenericArguments()[0];
        return true;
    }
}

/// <summary>
///     Provides an optional type that combines a <see cref="Value" /> and a <see cref="HasValue" /> which indicates
///     whether or not the <see cref="Value" /> is meaningful.
/// </summary>
public readonly struct Optional<TValue> : IEquatable<Optional<TValue>>
{
    public const string NoValueStringValue = "unspecified";
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
        HasValue = value is not null;
    }

    public Optional(Optional<TValue> value)
    {
        ValueOrDefault = value.ValueOrDefault;
        HasValue = value.HasValue;
    }

    public Optional(Optional<Optional<TValue>> value)
    {
        ValueOrDefault = value.ValueOrDefault.ValueOrDefault;
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
    public static implicit operator Optional<TValue>(TValue value)
    {
        return new Optional<TValue>(value);
    }

    /// <summary>
    ///     Casts the specified optional <see cref="value" /> to its contained value
    /// </summary>
    public static implicit operator TValue(Optional<TValue> value)
    {
        return value.Value;
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

        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
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