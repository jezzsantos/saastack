using Application.Persistence.Common;
using Common;
using IdentityDomain;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("SSOUser")]
public class SSOUser : ReadModelEntity
{
    public Optional<string> CountryCode { get; set; }

    public Optional<string> EmailAddress { get; set; }

    public Optional<string> FirstName { get; set; }

    public Optional<string> LastName { get; set; }

    public Optional<string> ProviderName { get; set; }

    public Optional<string> Timezone { get; set; }

    public Optional<SSOAuthTokens> Tokens { get; set; }

    public Optional<string> UserId { get; set; }
}