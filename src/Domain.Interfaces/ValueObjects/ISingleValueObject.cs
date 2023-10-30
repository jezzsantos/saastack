namespace Domain.Interfaces.ValueObjects;

/// <summary>
///     Defines a value object with a single value
/// </summary>
public interface ISingleValueObject<out TValue> : IValueObject
{
    TValue Value { get; }
}