using Domain.Common.ValueObjects;
using Domain.Events.Shared.Signings;

namespace SigningsDomain;

public static class Events
{
    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created(id)
        {
            OrganizationId = organizationId
        };
    }
}