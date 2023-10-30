using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

/// <inheritdoc cref="SingleValueObjectBase{TValueObject,TValue}" />
public partial class SingleValueObjectBase<TValueObject, TValue>
{
    [SkipImmutabilityCheck]
    public override bool Equals(object? other)
    {
        return base.Equals(other);
    }

    [SkipImmutabilityCheck]
    public override int GetHashCode()
    {
        if (Value.NotExists())
        {
            return 0;
        }

        if (Value is string stringValue)
        {
            return GetDeterministicHashCode(stringValue);
        }

        return Value.GetHashCode();
    }

    public static bool operator ==(SingleValueObjectBase<TValueObject, TValue> left,
        SingleValueObjectBase<TValueObject, TValue> right)
    {
        if (left.NotExists())
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator ==(SingleValueObjectBase<TValueObject, TValue> left, TValue? right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (left.Value.NotExists())
        {
            return false;
        }

        return left.Value.Equals(right);
    }

    public static bool operator !=(SingleValueObjectBase<TValueObject, TValue> left,
        SingleValueObjectBase<TValueObject, TValue> right)
    {
        return !(left == right);
    }

    public static bool operator !=(SingleValueObjectBase<TValueObject, TValue> left, TValue? right)
    {
        return !(left == right);
    }
}