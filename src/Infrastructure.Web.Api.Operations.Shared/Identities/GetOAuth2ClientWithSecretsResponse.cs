using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetOAuth2ClientWithSecretsResponse : IWebResponse
{
    public required OAuth2ClientWithSecrets Client { get; set; }
}