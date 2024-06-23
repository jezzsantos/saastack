using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class MemberUnInvited : DomainEvent
{
    public MemberUnInvited(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MemberUnInvited()
    {
    }

    public required string UninvitedById { get; set; }

    public required string UninvitedId { get; set; }
}