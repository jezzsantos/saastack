using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/tokens/{RefreshToken}", OperationMethod.Delete)]
public class RevokeRefreshTokenRequest : UnTenantedDeleteRequest
{
    public required string RefreshToken { get; set; }
}