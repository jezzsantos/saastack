using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class EndUser : IIdentifiableResource
{
    public EndUserAccess Access { get; set; }

    public EndUserClassification Classification { get; set; }

    public List<string> Features { get; set; } = new();

    public List<string> Roles { get; set; } = new();

    public EndUserStatus Status { get; set; }

    public required string Id { get; set; }
}

public enum EndUserStatus
{
    Unregistered = 0,
    Registered = 1
}

public enum EndUserAccess
{
    Enabled = 0,
    Suspended = 1
}

public enum EndUserClassification
{
    Person = 0,
    Machine = 1
}

public class EndUserWithMemberships : EndUser
{
    public List<Membership> Memberships { get; set; } = new();
}

public class Membership : IIdentifiableResource
{
    public List<string> Features { get; set; } = new();

    public bool IsDefault { get; set; }

    public required string OrganizationId { get; set; }

    public OrganizationOwnership Ownership { get; set; }

    public List<string> Roles { get; set; } = new();

    public required string UserId { get; set; }

    public required string Id { get; set; }
}

public class MembershipWithUserProfile : Membership
{
    public required UserProfile Profile { get; set; }

    public EndUserStatus Status { get; set; }
}

public class Invitation
{
    public required string EmailAddress { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }
}