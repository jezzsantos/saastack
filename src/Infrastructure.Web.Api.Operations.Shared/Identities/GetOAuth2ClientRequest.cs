using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches an OAuth2/OIDC client
/// </summary>
[Route("/oauth2/clients/{Id}", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class GetOAuth2ClientRequest : UnTenantedRequest<GetOAuth2ClientRequest, GetOAuth2ClientResponse>
{
    [Required] public string? Id { get; set; }
}