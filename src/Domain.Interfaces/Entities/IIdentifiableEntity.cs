using Domain.Interfaces.ValueObjects;

namespace Domain.Interfaces.Entities;

/// <summary>
///     An entity with an identifier
/// </summary>
public interface IIdentifiableEntity
{
    Identifier Id { get; }
}