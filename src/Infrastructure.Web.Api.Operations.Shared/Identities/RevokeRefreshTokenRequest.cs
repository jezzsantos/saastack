using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("tokens/{RefreshToken}", ServiceOperation.Delete)]
public class RevokeRefreshTokenRequest : UnTenantedDeleteRequest
{
    public required string RefreshToken { get; set; }
}