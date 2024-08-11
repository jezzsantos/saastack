using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Lists all the available feature flags
/// </summary>
[Route("/flags", OperationMethod.Get, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class GetAllFeatureFlagsRequest : UnTenantedRequest<GetAllFeatureFlagsRequest, GetAllFeatureFlagsResponse>;