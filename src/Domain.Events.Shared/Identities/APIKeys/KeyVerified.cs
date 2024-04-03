using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class KeyVerified : IDomainEvent
{
    public required bool IsVerified { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}