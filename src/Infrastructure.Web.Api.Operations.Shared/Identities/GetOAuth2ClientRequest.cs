using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches an OAuth2/Open ID Connect client
/// </summary>
[Route("/oauth2/clients/{Id}", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class GetOAuth2ClientRequest : UnTenantedRequest<GetOAuth2ClientRequest, GetOAuth2ClientWithSecretsResponse>
{
    [Required] public string? Id { get; set; }
}