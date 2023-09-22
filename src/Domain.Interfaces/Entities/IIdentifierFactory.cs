using Domain.Interfaces.ValueObjects;

namespace Domain.Interfaces.Entities;

/// <summary>
///     A factory that generates identifiers
/// </summary>
public interface IIdentifierFactory
{
    Identifier Create(IIdentifiableEntity entity);

    bool IsValid(Identifier value);
}