using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class GuestInvitationAccepted : IDomainEvent
{
    public required DateTime AcceptedAtUtc { get; set; }

    public required string AcceptedEmailAddress { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}