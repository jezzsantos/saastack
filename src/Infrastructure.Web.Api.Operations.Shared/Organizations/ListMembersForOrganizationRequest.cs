using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/members", ServiceOperation.Search, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class ListMembersForOrganizationRequest : UnTenantedSearchRequest<ListMembersForOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}