using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

/// <inheritdoc cref="ValueObjectBase{TValueObject}" />
public abstract partial class ValueObjectBase<TValueObject> : IComparable, IComparable<ValueObjectBase<TValueObject>>
{
    [SkipImmutabilityCheck]
    public int CompareTo(object? other)
    {
        if (other.NotExists()
            || other.GetType() != GetType())
        {
            return -1;
        }

        return CompareTo((ValueObjectBase<TValueObject>)other);
    }

    [SkipImmutabilityCheck]
    public int CompareTo(ValueObjectBase<TValueObject>? other)
    {
        if (other.NotExists())
        {
            return -1;
        }

        var thisValue = Dehydrate();
        var otherValue = other.Dehydrate();
        return string.Compare(thisValue, otherValue, StringComparison.Ordinal);
    }

    public static bool operator >(ValueObjectBase<TValueObject> left, ValueObjectBase<TValueObject> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(ValueObjectBase<TValueObject> left, ValueObjectBase<TValueObject> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        return left.CompareTo(right) >= 0;
    }

    public static bool operator <(ValueObjectBase<TValueObject> left, ValueObjectBase<TValueObject> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(ValueObjectBase<TValueObject> left, ValueObjectBase<TValueObject> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        return left.CompareTo(right) <= 0;
    }
}