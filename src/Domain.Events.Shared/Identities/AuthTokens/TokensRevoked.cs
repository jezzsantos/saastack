using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.AuthTokens;

public sealed class TokensRevoked : IDomainEvent
{
    public required string UserId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}