using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;

public sealed class TokenRefreshed : DomainEvent
{
    public TokenRefreshed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TokenRefreshed()
    {
    }

    public required string AccessTokenDigest { get; set; }

    public DateTime? AccessTokenExpiresOn { get; set; }

    public required DateTime RefreshedAt { get; set; }

    public required string RefreshTokenDigest { get; set; }

    public DateTime? RefreshTokenExpiresOn { get; set; }

    public required List<string> Scopes { get; set; }
}