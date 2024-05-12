using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Lists all the available feature flags
/// </summary>
[Route("/flags", OperationMethod.Get)]
public class GetAllFeatureFlagsRequest : UnTenantedRequest<GetAllFeatureFlagsResponse>;