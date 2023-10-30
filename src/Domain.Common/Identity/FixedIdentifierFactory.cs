using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Common.Identity;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that creates identifiers using a provided value
/// </summary>
public class FixedIdentifierFactory : IIdentifierFactory
{
    private readonly Identifier _id;

    public FixedIdentifierFactory(string identifier)
    {
        _id = identifier.ToId();
    }

    public Result<Identifier, Error> Create(IIdentifiableEntity entity)
    {
        return _id;
    }

    public bool IsValid(Identifier value)
    {
        return _id == value;
    }
}