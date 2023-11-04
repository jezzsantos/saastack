namespace Domain.Interfaces;

/// <summary>
///     Defines an interface for an object whose state can be created from a set of properties
/// </summary>
public interface IRehydratableObject
{
    void Rehydrate();
}