using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     Assigns the specified roles to the specified user
/// </summary>
[Route("/users/{Id}/roles", OperationMethod.Post, AccessType.Token)]
[Authorize(Interfaces.Roles.Platform_Operations)]
public class AssignPlatformRolesRequest : UnTenantedRequest<UpdateUserResponse>
{
    [Required] public string? Id { get; set; }

    public List<string>? Roles { get; set; }
}