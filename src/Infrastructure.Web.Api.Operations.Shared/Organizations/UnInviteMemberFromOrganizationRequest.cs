using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Removes a previously invited member from the organization
/// </summary>
[Route("/organizations/{Id}/members/{UserId}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Platform_PaidTrial)]
public class UnInviteMemberFromOrganizationRequest : UnTenantedRequest<UnInviteMemberFromOrganizationRequest,
        UnInviteMemberFromOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    [Required] public string? UserId { get; set; }

    public string? Id { get; set; }
}