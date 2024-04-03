using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.AuthTokens;

public sealed class TokensChanged : DomainEvent
{
    public TokensChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TokensChanged()
    {
    }

    public required string AccessToken { get; set; }

    public required DateTime AccessTokenExpiresOn { get; set; }

    public required string RefreshToken { get; set; }

    public required DateTime RefreshTokenExpiresOn { get; set; }

    public required string UserId { get; set; }
}