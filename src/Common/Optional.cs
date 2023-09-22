using Common.Extensions;

namespace Common;

/// <summary>
///     Provides an optional type that combines a <see cref="Value" /> and a <see cref="HasValue" /> which indicates
///     whether or not the &lt;see cref="Value" /&gt; is meaningful.
/// </summary>
public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    public const string NoValueStringValue = "unspecified";
    private const string NullValueStringValue = "null";

    public Optional(T value)
    {
        HasValue = value.Exists();
        Value = value.Exists() ? value : default!;
    }

    public bool HasValue { get; }

    public T Value { get; }

    public static implicit operator Optional<T>(T value)
    {
        return new Optional<T>(value);
    }

    public override string ToString()
    {
        return HasValue
            ? Value?.ToString() ?? NullValueStringValue
            : NoValueStringValue;
    }

    public bool Equals(Optional<T> other)
    {
        return EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Optional<T> other)
        {
            return Equals(other);
        }

        if (obj is T instance)
        {
            return Equals(instance);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HasValue
            ? EqualityComparer<T>.Default.GetHashCode(Value!)
            : 0;
    }

    public static bool operator ==(Optional<T> left, Optional<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Optional<T> left, Optional<T> right)
    {
        return !left.Equals(right);
    }

    public static bool operator ==(Optional<T> left, T right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Optional<T> left, T right)
    {
        if (right.NotExists())
        {
            return left.NotExists();
        }

        return !(left == right!);
    }

    public static bool operator ==(T left, Optional<T> right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(T left, Optional<T> right)
    {
        if (left.NotExists())
        {
            return right.NotExists();
        }

        return !(right == left!);
    }
}