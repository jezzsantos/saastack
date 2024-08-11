using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Invites a new member to the organization, either by email address, or by their user ID (if known)
/// </summary>
[Route("/organizations/{Id}/members", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Platform_PaidTrial)]
public class InviteMemberToOrganizationRequest :
    UnTenantedRequest<InviteMemberToOrganizationRequest, InviteMemberToOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Email { get; set; }

    public string? UserId { get; set; }

    public string? Id { get; set; }
}