using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("tokens/refresh", ServiceOperation.Post)]
public class RefreshTokenRequest : UnTenantedRequest<RefreshTokenResponse>
{
    public required string RefreshToken { get; set; }
}