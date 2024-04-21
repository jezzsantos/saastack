using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_PaidTrial)]
public class DeleteOrganizationRequest : UnTenantedDeleteRequest, IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}