using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/users/{Id}/roles", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Interfaces.Roles.Platform_Operations)]
public class UnassignPlatformRolesRequest : UnTenantedRequest<GetUserResponse>
{
    [Required] public string? Id { get; set; }

    public List<string>? Roles { get; set; }
}