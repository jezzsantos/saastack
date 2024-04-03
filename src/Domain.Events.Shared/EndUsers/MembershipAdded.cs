using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipAdded : IDomainEvent
{
    public required List<string> Features { get; set; }

    public required bool IsDefault { get; set; }

    public string? MembershipId { get; set; }

    public required string OrganizationId { get; set; }

    public required List<string> Roles { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}