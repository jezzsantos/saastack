using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipDefaultChanged : IDomainEvent
{
    public required string FromMembershipId { get; set; }

    public required string ToMembershipId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}