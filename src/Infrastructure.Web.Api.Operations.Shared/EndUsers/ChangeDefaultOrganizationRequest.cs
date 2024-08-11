using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     Changes the default organization for the authenticated user
/// </summary>
[Route("/memberships/me/default", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class ChangeDefaultOrganizationRequest : UnTenantedRequest<ChangeDefaultOrganizationRequest, UpdateUserResponse>
{
    [Required] public string? OrganizationId { get; set; }
}