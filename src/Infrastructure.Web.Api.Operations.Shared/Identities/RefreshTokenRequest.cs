using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Refreshes the access_token for the specified refresh_token
/// </summary>
[Route("/tokens/refresh", OperationMethod.Post)]
public class RefreshTokenRequest : UnTenantedRequest<RefreshTokenRequest, RefreshTokenResponse>
{
    [Required] public string? RefreshToken { get; set; }
}