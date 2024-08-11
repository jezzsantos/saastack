using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Fetches the profile of the organization
/// </summary>
[Route("/organizations/{Id}", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class GetOrganizationRequest : UnTenantedRequest<GetOrganizationRequest, GetOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}