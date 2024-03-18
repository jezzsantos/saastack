using Application.Interfaces.Resources;
using JetBrains.Annotations;

namespace Application.Resources.Shared;

public class Organization : IIdentifiableResource
{
    public required string CreatedById { get; set; }

    public required string Name { get; set; }

    public OrganizationOwnership Ownership { get; set; }

    public required string Id { get; set; }
}

[UsedImplicitly]
public class OrganizationWithSettings : Organization
{
    public required Dictionary<string, string> Settings { get; set; }
}

public enum OrganizationOwnership
{
    Shared = 0,
    Personal = 1
}