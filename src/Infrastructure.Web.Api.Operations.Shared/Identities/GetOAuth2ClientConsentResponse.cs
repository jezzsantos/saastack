using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetOAuth2ClientConsentResponse : IWebResponse
{
    public required OAuth2ClientConsent Consent { get; set; }
}