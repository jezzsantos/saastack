using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Revokes the API key
/// </summary>
[Route("/apikeys/{Id}/revoke", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class RevokeAPIKeyRequest : UnTenantedDeleteRequest<RevokeAPIKeyRequest>
{
    [Required] public string? Id { get; set; }
}