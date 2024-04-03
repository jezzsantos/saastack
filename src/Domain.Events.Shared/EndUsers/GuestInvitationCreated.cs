using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class GuestInvitationCreated : IDomainEvent
{
    public required string EmailAddress { get; set; }

    public required string InvitedById { get; set; }

    public required string Token { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}