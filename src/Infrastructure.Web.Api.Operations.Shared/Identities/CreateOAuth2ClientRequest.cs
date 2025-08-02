using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Creates a new OAuth2/Open ID Connect client application
/// </summary>
[Route("/oauth2/clients", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class CreateOAuth2ClientRequest : UnTenantedRequest<CreateOAuth2ClientRequest, GetOAuth2ClientResponse>
{
    [Required] public string? Name { get; set; }

    public string? RedirectUri { get; set; }
}