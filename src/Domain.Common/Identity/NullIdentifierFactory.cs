﻿using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Common.Identity;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that generates empty <see cref="Identifier" />
/// </summary>
public class NullIdentifierFactory : IIdentifierFactory
{
    public Result<Identifier, Error> Create(IIdentifiableEntity entity)
    {
        return Identifier.Empty();
    }

    public bool IsValid(Identifier value)
    {
        throw new NotImplementedException();
    }
}