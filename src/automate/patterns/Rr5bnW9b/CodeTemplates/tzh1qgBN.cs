using Domain.Common.ValueObjects;
using Domain.Events.Shared.{{SubdomainName | string.pascalplural}};

namespace {{SubdomainName | string.pascalplural}}Domain;

public static class Events
{
    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created(id)
        {
            OrganizationId = organizationId,
            //TODO: add assignments for other properties
        };
    }
}