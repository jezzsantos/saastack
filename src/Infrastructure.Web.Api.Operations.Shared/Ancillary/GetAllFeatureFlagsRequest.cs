using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/flags", OperationMethod.Get, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class GetAllFeatureFlagsRequest : UnTenantedRequest<GetAllFeatureFlagsResponse>;