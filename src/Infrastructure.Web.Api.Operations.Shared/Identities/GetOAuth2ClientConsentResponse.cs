using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetOAuth2ClientConsentResponse : IWebResponse
{
    public required bool Consented { get; set; }
}