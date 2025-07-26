using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Revokes the user's consent for an OAuth2/OIDC client
/// </summary>
[Route("/oauth2/clients/{Id}/consent/revoke", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class
    RevokeOAuth2ClientConsentForCallerRequest : UnTenantedRequest<RevokeOAuth2ClientConsentForCallerRequest,
    GetOAuth2ClientConsentResponse>
{
    [Required] public string? Id { get; set; }
}