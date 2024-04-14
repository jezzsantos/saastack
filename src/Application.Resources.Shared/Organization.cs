using Application.Interfaces.Resources;
using JetBrains.Annotations;

namespace Application.Resources.Shared;

public class Organization : IIdentifiableResource
{
    public required string CreatedById { get; set; }

    public required string Name { get; set; }

    public OrganizationOwnership Ownership { get; set; }

    public required string Id { get; set; }

    public string? AvatarUrl { get; set; }
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

public class OrganizationMember : IIdentifiableResource
{
    public UserProfileClassification Classification { get; set; }

    public string? EmailAddress { get; set; }

    public List<string> Features { get; set; } = new();

    public bool IsDefault { get; set; }

    public bool IsOwner { get; set; }

    public bool IsRegistered { get; set; }

    public required PersonName Name { get; set; }

    public List<string> Roles { get; set; } = new();

    public required string UserId { get; set; }

    public required string Id { get; set; }
}