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

    public string? EmailAddress { get; set; }

    public required string InvitedById { get; set; }

    public string? UserId { get; set; }
}