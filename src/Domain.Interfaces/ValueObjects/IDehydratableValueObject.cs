namespace Domain.Interfaces.ValueObjects;

/// <summary>
///     Defines an value object that can persist its state to single string
/// </summary>
public interface IDehydratableValueObject
{
    string Dehydrate();
}