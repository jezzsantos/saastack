using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Fetches the named feature flag, for all users, or for a specific user, and optionally for a specific tenancy
/// </summary>
[Route("/flags/{UserId}/{Name}", OperationMethod.Get, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class GetFeatureFlagRequest : UnTenantedRequest<GetFeatureFlagRequest, GetFeatureFlagResponse>
{
    [Required] public string? Name { get; set; }

    public string? TenantId { get; set; }

    [Required] public string? UserId { get; set; }
}