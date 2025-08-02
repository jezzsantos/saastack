using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Updates an existing OAuth2/Open ID Connect client
/// </summary>
[Route("/oauth2/clients/{Id}", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class UpdateOAuth2ClientRequest : UnTenantedRequest<UpdateOAuth2ClientRequest, GetOAuth2ClientResponse>
{
    [Required] public string? Id { get; set; }

    public string? Name { get; set; }

    public string? RedirectUri { get; set; }
}