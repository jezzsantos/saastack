using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Deletes an OAuth2/OIDC client
/// </summary>
[Route("/oauth2/clients/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class DeleteOAuth2ClientRequest : UnTenantedDeleteRequest<DeleteOAuth2ClientRequest>
{
    [Required] public string? Id { get; set; }
}