using Application.Persistence.Common;
using Common;
using IdentityDomain;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("OAuth2ClientConsent")]
public class OAuth2ClientConsent : ReadModelEntity
{
    public Optional<string> ClientId { get; set; }

    public Optional<bool> IsConsented { get; set; }

    public Optional<OAuth2Scopes> Scopes { get; set; }

    public Optional<string> UserId { get; set; }
}