using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Changes the profile of the organization
/// </summary>
[Route("/organizations/{Id}", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class ChangeOrganizationRequest : UnTenantedRequest<ChangeOrganizationRequest, GetOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Name { get; set; }

    public string? Id { get; set; }
}