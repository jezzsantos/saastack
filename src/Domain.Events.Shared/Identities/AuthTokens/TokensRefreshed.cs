using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.AuthTokens;

public sealed class TokensRefreshed : IDomainEvent
{
    public required string AccessToken { get; set; }

    public required DateTime AccessTokenExpiresOn { get; set; }

    public required string RefreshToken { get; set; }

    public required DateTime RefreshTokenExpiresOn { get; set; }

    public required string UserId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}