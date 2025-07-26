using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RegenerateOAuth2ClientSecretResponse : IWebResponse
{
    public required OAuth2ClientWithSecret? Client { get; set; }
}