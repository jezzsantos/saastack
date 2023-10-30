using Common.Extensions;

namespace Domain.Common.ValueObjects;

/// <inheritdoc cref="SingleValueObjectBase{TValueObject,TValue}" />
public partial class SingleValueObjectBase<TValueObject, TValue>
{
    public static bool operator >(SingleValueObjectBase<TValueObject, TValue> left,
        SingleValueObjectBase<TValueObject, TValue> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right.Value) > 0;
        }

        return false;
    }

    public static bool operator >(SingleValueObjectBase<TValueObject, TValue> left, TValue? right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right) > 0;
        }

        return false;
    }

    public static bool operator >=(SingleValueObjectBase<TValueObject, TValue> left,
        SingleValueObjectBase<TValueObject, TValue> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right.Value) >= 0;
        }

        return false;
    }

    public static bool operator >=(SingleValueObjectBase<TValueObject, TValue> left, TValue? right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right) >= 0;
        }

        return false;
    }

    public static bool operator <(SingleValueObjectBase<TValueObject, TValue> left,
        SingleValueObjectBase<TValueObject, TValue> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right.Value) < 0;
        }

        return false;
    }

    public static bool operator <(SingleValueObjectBase<TValueObject, TValue> left, TValue? right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right) < 0;
        }

        return false;
    }

    public static bool operator <=(SingleValueObjectBase<TValueObject, TValue> left,
        SingleValueObjectBase<TValueObject, TValue> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right.Value) <= 0;
        }

        return false;
    }

    public static bool operator <=(SingleValueObjectBase<TValueObject, TValue> left, TValue? right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.Value is IComparable comparable)
        {
            return comparable.CompareTo(right) <= 0;
        }

        return false;
    }
}