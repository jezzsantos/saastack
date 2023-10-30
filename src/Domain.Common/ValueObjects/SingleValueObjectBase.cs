using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

/// <summary>
///     Defines a DDD value object (for a single value).
///     Value objects are immutable, and their properties should be set at construction, and never altered.
///     Value objects are equal when their internal data is the same.
///     Value objects support being persisted
/// </summary>
public abstract partial class SingleValueObjectBase<TValueObject, TValue> : ValueObjectBase<TValueObject>,
    ISingleValueObject<TValue>
    where TValue : notnull
{
    protected SingleValueObjectBase(TValue value)
    {
        Value = value;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { Value };
    }

    TValue ISingleValueObject<TValue>.Value => Value;

    protected TValue Value { get; }

    public static implicit operator TValue(SingleValueObjectBase<TValueObject, TValue> valueObject)
    {
        return valueObject.NotExists() || valueObject.Value.NotExists()
            ? default!
            : valueObject.Value;
    }
}