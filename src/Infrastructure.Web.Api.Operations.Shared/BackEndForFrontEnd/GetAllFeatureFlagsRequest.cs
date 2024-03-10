using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/flags", ServiceOperation.Get)]
public class GetAllFeatureFlagsRequest : UnTenantedRequest<GetAllFeatureFlagsResponse>;