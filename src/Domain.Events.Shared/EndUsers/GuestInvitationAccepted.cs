using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class GuestInvitationAccepted : DomainEvent
{
    public GuestInvitationAccepted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public GuestInvitationAccepted()
    {
    }

    public required DateTime AcceptedAtUtc { get; set; }

    public required string AcceptedEmailAddress { get; set; }
}