using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class CreateOrganizationRequest : UnTenantedRequest<GetOrganizationResponse>
{
    public required string Name { get; set; }
}