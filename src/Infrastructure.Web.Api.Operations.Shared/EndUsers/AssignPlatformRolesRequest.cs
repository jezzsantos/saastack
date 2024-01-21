using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/users/{id}/roles", ServiceOperation.Post, AccessType.Token)]
[Authorize(Interfaces.Roles.Platform_Operations)]
public class AssignPlatformRolesRequest : UnTenantedRequest<AssignPlatformRolesResponse>
{
    public required string Id { get; set; }

    public List<string>? Roles { get; set; }
}