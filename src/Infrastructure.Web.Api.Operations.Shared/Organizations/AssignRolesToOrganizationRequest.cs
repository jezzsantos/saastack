using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Assigns a list of roles to a member of an organization
/// </summary>
[Route("/organizations/{Id}/roles/assign", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Interfaces.Roles.Tenant_Owner, Features.Tenant_PaidTrial)]
public class AssignRolesToOrganizationRequest :
    UnTenantedRequest<AssignRolesToOrganizationRequest, GetOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public List<string> Roles { get; set; } = [];

    [Required] public string? UserId { get; set; }

    public string? Id { get; set; }
}