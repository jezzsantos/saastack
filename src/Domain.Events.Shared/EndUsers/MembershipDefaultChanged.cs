using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipDefaultChanged : DomainEvent
{
    public MembershipDefaultChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipDefaultChanged()
    {
    }

    public required string FromMembershipId { get; set; }

    public required string ToMembershipId { get; set; }
}