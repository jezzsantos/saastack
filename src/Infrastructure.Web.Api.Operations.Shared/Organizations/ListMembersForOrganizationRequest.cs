using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Fetches all the members of the organization
/// </summary>
[Route("/organizations/{Id}/members", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class ListMembersForOrganizationRequest : UnTenantedSearchRequest<ListMembersForOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}