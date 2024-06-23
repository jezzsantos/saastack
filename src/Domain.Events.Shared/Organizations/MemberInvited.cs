using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class MemberInvited : DomainEvent
{
    public MemberInvited(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MemberInvited()
    {
    }

    public string? EmailAddress { get; set; }

    public required string InvitedById { get; set; }

    public string? InvitedId { get; set; }
}