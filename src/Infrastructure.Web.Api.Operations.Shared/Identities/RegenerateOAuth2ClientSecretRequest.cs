using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Regenerates the client secret for an OAuth2/Open ID Connect client
/// </summary>
[Route("/oauth2/clients/{Id}/secret", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class RegenerateOAuth2ClientSecretRequest : UnTenantedRequest<RegenerateOAuth2ClientSecretRequest,
    RegenerateOAuth2ClientSecretResponse>
{
    [Required] public string? Id { get; set; }
}