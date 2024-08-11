using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Removes the avatar of the organization
/// </summary>
[Route("/organizations/{Id}/avatar", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class DeleteOrganizationAvatarRequest :
    UnTenantedRequest<DeleteOrganizationAvatarRequest, GetOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}