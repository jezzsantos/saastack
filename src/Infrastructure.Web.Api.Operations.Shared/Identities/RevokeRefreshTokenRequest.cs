using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Revoke a valid refresh_token
/// </summary>
[Route("/tokens/{RefreshToken}", OperationMethod.Delete)]
public class RevokeRefreshTokenRequest : UnTenantedDeleteRequest
{
    [Required] public string? RefreshToken { get; set; }
}