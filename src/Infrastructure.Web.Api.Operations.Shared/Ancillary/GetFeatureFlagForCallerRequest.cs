using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/flags/{Name}", OperationMethod.Get)]
public class GetFeatureFlagForCallerRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    public required string Name { get; set; }
}