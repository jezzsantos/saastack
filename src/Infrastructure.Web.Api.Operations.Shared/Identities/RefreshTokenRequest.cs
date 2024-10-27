using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Refreshes the access_token for the specified refresh_token
/// </summary>
/// <response code="401">The refresh token has expired</response>
/// <response code="423">The user's account is suspended or disabled, and cannot be authenticated or used</response>
[Route("/tokens/refresh", OperationMethod.Post)]
public class RefreshTokenRequest : UnTenantedRequest<RefreshTokenRequest, RefreshTokenResponse>
{
    [Required] public string? RefreshToken { get; set; }
}