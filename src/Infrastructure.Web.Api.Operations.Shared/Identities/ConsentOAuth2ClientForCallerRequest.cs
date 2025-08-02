using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Consent for the user to authorize the OAuth2/Open ID Connect client to access their data
/// </summary>
[Route("/oauth2/clients/{Id}/consent", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class
    ConsentOAuth2ClientForCallerRequest : UnTenantedRequest<ConsentOAuth2ClientForCallerRequest,
    GetOAuth2ClientConsentResponse>
{
    [Required] public bool Consented { get; set; }

    [Required] public string? Id { get; set; }

    public string? RedirectUri { get; set; }

    [Required] public string? Scope { get; set; }

    public string? State { get; set; }
}