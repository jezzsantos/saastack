using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Common.Identity;

/// <summary>
///     A factory that generates identifiers
/// </summary>
public interface IIdentifierFactory
{
    Result<Identifier, Error> Create(IIdentifiableEntity entity);

    bool IsValid(Identifier value);
}