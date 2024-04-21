using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class MembershipRemoved : DomainEvent
{
    public MembershipRemoved(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipRemoved()
    {
    }

    public required string UserId { get; set; }
}