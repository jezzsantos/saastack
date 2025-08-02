using Application.Persistence.Common;
using Common;
using Domain.Shared.Identities;
using IdentityDomain;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("OpenIdConnectAuthorization")]
public class OpenIdConnectAuthorization : ReadModelEntity
{
    public Optional<string> AccessTokenDigest { get; set; }

    public Optional<DateTime> AccessTokenExpiresAt { get; set; }

    public Optional<string> AuthorizationCode { get; set; }

    public Optional<DateTime> AuthorizationExpiresAt { get; set; }

    public Optional<DateTime> AuthorizedAt { get; set; }

    public Optional<string> ClientId { get; set; }

    public Optional<string> CodeChallenge { get; set; }

    public Optional<OpenIdConnectCodeChallengeMethod> CodeChallengeMethod { get; set; }

    public Optional<DateTime> CodeExchangedAt { get; set; }

    public Optional<DateTime> LastRefreshedAt { get; set; }

    public Optional<string> Nonce { get; set; }

    public Optional<string> RedirectUri { get; set; }

    public Optional<string> RefreshTokenDigest { get; set; }

    public Optional<DateTime> RefreshTokenExpiresAt { get; set; }

    public Optional<OAuth2Scopes> Scopes { get; set; }

    public Optional<string> UserId { get; set; }
}