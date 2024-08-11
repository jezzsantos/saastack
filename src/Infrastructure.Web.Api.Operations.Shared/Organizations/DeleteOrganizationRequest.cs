using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Deletes the organization
/// </summary>
[Route("/organizations/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_PaidTrial)]
public class DeleteOrganizationRequest : UnTenantedDeleteRequest<DeleteOrganizationRequest>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}