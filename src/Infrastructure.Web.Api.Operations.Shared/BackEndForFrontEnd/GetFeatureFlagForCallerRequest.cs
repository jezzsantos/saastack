using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/flags/{Name}", OperationMethod.Get)]
public class GetFeatureFlagForCallerRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    public required string Name { get; set; }
}