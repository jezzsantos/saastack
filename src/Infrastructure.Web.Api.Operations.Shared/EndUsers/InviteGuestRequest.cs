using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/invitations", ServiceOperation.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class InviteGuestRequest : UnTenantedRequest<InviteGuestResponse>
{
    public required string Email { get; set; }
}