using Application.Persistence.Common;
using Common;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("OAuth2Client")]
public class OAuth2Client : ReadModelEntity
{
    public Optional<string> Name { get; set; }

    public Optional<string> RedirectUri { get; set; }
}