using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipRemoved : DomainEvent
{
    public MembershipRemoved(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipRemoved()
    {
    }

    public required string MembershipId { get; set; }

    public required string OrganizationId { get; set; }

    public required string UnInvitedById { get; set; }
}