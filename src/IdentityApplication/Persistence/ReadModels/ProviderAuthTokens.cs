using Application.Persistence.Common;
using Common;
using IdentityDomain;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("ProviderAuthTokens")]
public class ProviderAuthTokens : ReadModelEntity
{
    public Optional<string> ProviderName { get; set; }

    public Optional<AuthTokens> Tokens { get; set; }

    public Optional<string> UserId { get; set; }
}