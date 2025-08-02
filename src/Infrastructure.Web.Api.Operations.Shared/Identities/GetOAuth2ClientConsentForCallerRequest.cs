using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Gets the user's consent status for an OAuth2/Open ID Connect client
/// </summary>
[Route("/oauth2/clients/{Id}/consent", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class
    GetOAuth2ClientConsentForCallerRequest : UnTenantedRequest<GetOAuth2ClientConsentForCallerRequest,
    GetOAuth2ClientConsentResponse>
{
    [Required] public string? Id { get; set; }
}