using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipFeatureAssigned : IDomainEvent
{
    public required string Feature { get; set; }

    public required string MembershipId { get; set; }

    public required string OrganizationId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}