using Common.Extensions;

namespace Common;

/// <summary>
///     Provides an optional type that combines a <see cref="Value" /> and a <see cref="HasValue" /> which indicates
///     whether or not the <see cref="Value" /> is meaningful.
/// </summary>
public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    public const string NoValueStringValue = "unspecified";
    private const string NullValueStringValue = "null";

    /// <summary>
    ///     Returns the container without any value
    /// </summary>
    public static Optional<T> None => default;

    public Optional(T? value)
    {
        ValueOrDefault = value;
        HasValue = value is not null;
    }

    /// <summary>
    ///     Whether there is a value
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    ///     Returns the contained value
    /// </summary>
    public T Value
    {
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
    public T? ValueOrDefault { get; }

    /// <summary>
    ///     Tries to obtain the value
    /// </summary>
    public bool TryGet(out T value)
    {
        value = ValueOrDefault!;
        return HasValue;
    }

    /// <summary>
    ///     Returns a new <see cref="Optional{T}" /> containing the value
    /// </summary>
    public static implicit operator Optional<T>(T value)
    {
        return new Optional<T>(value);
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
    public bool Equals(Optional<T> other)
    {
        if (!HasValue)
        {
            return !other.HasValue;
        }

        return EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    /// <summary>
    ///     Whether this instance contains the same value as the <see cref="other" /> instance
    /// </summary>
    public override bool Equals(object? other)
    {
        if (other is Optional<T> optional)
        {
            return Equals(optional);
        }

        if (other is T instance)
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
            ? EqualityComparer<T>.Default.GetHashCode(Value!)
            : 0;
    }

    /// <summary>
    ///     Whether the <see cref="left" /> contains the same value as the contained value from the <see cref="right" />
    /// </summary>
    public static bool operator ==(Optional<T> left, Optional<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> contains a different value from the contained value of the <see cref="right" />
    /// </summary>
    public static bool operator !=(Optional<T> left, Optional<T> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> <see cref="Optional{T}" /> contains the same value as the <see cref="right" />
    ///     instance
    /// </summary>
    public static bool operator ==(Optional<T> left, T right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> <see cref="Optional{T}" /> contains a different value from the <see cref="right" />
    ///     instance
    /// </summary>
    public static bool operator !=(Optional<T> left, T right)
    {
        if (right.NotExists())
        {
            return left.NotExists();
        }

        return !(left == right!);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> instance is the same value as the contained value of the <see cref="right" />
    /// </summary>
    public static bool operator ==(T left, Optional<T> right)
    {
        return right.Equals(left);
    }

    /// <summary>
    ///     Whether the <see cref="left" /> instance is different from the contained value of the <see cref="right" />
    /// </summary>
    public static bool operator !=(T left, Optional<T> right)
    {
        if (left.NotExists())
        {
            return right.NotExists();
        }

        return !(right == left!);
    }
}