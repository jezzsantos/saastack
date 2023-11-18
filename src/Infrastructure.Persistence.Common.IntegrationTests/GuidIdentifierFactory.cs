using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Common.IntegrationTests;

internal class GuidIdentifierFactory : IIdentifierFactory
{
    public bool IsValid(Identifier value)
    {
        if (!value.HasValue())
        {
            return false;
        }

        if (!Guid.TryParse(value, out var result))
        {
            return false;
        }

        if (result == Guid.Empty)
        {
            return false;
        }

        return true;
    }

    public Result<Identifier, Error> Create(IIdentifiableEntity entity)
    {
        return Guid.NewGuid().ToString("D").ToId();
    }
}