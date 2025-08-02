using Application.Persistence.Common;
using Common;
using Domain.Shared.Identities;
using IdentityDomain;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("OidcAuthorization")]
public class OidcAuthorization : ReadModelEntity
{
    public Optional<string> AuthorizationCode { get; set; }

    public Optional<DateTime> AuthorizationExpiresAt { get; set; }

    public Optional<DateTime> AuthorizedAt { get; set; }

    public Optional<string> ClientId { get; set; }

    public Optional<string> CodeChallenge { get; set; }

    public Optional<OAuth2CodeChallengeMethod> CodeChallengeMethod { get; set; }

    public Optional<string> Nonce { get; set; }

    public Optional<string> RedirectUri { get; set; }

    public Optional<OAuth2Scopes> Scopes { get; set; }

    public Optional<string> UserId { get; set; }
}