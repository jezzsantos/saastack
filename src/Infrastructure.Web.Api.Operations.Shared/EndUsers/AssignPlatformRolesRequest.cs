using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/users/{Id}/roles", OperationMethod.Post, AccessType.Token)]
[Authorize(Interfaces.Roles.Platform_Operations)]
public class AssignPlatformRolesRequest : UnTenantedRequest<GetUserResponse>
{
    [Required] public string? Id { get; set; }

    public List<string>? Roles { get; set; }
}