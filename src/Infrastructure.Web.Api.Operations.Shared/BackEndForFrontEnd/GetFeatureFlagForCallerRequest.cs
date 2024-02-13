using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/flags/{Name}", ServiceOperation.Get)]
public class GetFeatureFlagForCallerRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    public required string Name { get; set; }
}