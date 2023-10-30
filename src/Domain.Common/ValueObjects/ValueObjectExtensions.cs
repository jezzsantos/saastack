using Common.Extensions;

namespace Domain.Common.ValueObjects;

public static class ValueObjectExtensions
{
    /// <summary>
    ///     Whether the <see cref="valueObject" /> has a null value or not
    /// </summary>
    public static bool HasValue<TValue>(this ValueObjectBase<TValue> valueObject)
    {
        if (valueObject.NotExists())
        {
            return false;
        }

        return valueObject != (ValueObjectBase<TValue>)null!
               && valueObject != ValueObjectBase<TValue>.NullValue;
    }
}