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

public class RegisteredEndUser : EndUser
{
    public ProfileWithDefaultMembership? Profile { get; set; }
}

public class EndUserWithMemberships : EndUser
{
    public List<Membership> Memberships { get; set; } = new();
}

public class Membership : IIdentifiableResource
{
    public List<string> Features { get; set; } = new();

    public required string OrganizationId { get; set; }

    public List<string> Roles { get; set; } = new();

    public required string Id { get; set; }
}