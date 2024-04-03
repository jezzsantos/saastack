using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class GuestInvitationCreated : DomainEvent
{
    public GuestInvitationCreated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public GuestInvitationCreated()
    {
    }

    public required string EmailAddress { get; set; }

    public required string InvitedById { get; set; }

    public required string Token { get; set; }
}