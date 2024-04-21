using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/members/{UserId}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Platform_PaidTrial)]
public class UnInviteMemberFromOrganizationRequest : UnTenantedRequest<UnInviteMemberFromOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public required string UserId { get; set; }

    public string? Id { get; set; }
}