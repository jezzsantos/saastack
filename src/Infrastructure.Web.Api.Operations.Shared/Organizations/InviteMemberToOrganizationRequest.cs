using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/members", ServiceOperation.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class InviteMemberToOrganizationRequest : UnTenantedRequest<InviteMemberToOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Email { get; set; }

    public string? UserId { get; set; }

    public string? Id { get; set; }
}