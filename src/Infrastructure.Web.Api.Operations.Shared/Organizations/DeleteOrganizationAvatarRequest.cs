using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/avatar", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class DeleteOrganizationAvatarRequest : UnTenantedRequest<GetOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}