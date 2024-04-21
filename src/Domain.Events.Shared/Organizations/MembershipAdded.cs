using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class MembershipAdded : DomainEvent
{
    public MembershipAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipAdded()
    {
    }

    public required string UserId { get; set; }
}