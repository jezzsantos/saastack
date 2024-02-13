using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/flags/{UserId}/{Name}", ServiceOperation.Get, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class GetFeatureFlagRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    public required string Name { get; set; }

    public string? TenantId { get; set; }

    public required string UserId { get; set; }
}