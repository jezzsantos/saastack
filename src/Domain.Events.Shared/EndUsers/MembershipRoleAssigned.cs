using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipRoleAssigned : IDomainEvent
{
    public required string MembershipId { get; set; }

    public required string OrganizationId { get; set; }

    public required string Role { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}