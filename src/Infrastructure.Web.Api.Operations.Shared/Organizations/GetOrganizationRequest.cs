using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}", ServiceOperation.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard)]
public class GetOrganizationRequest : UnTenantedRequest<GetOrganizationResponse>, IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}