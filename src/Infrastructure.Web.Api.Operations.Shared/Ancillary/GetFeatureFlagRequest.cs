using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/flags/{UserId}/{Name}", OperationMethod.Get, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class GetFeatureFlagRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    [Required] public string? Name { get; set; }

    public string? TenantId { get; set; }

    [Required] public string? UserId { get; set; }
}