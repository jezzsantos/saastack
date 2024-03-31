using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/invitations/{Token}/resend", ServiceOperation.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class ResendGuestInvitationRequest : UnTenantedEmptyRequest
{
    public required string Token { get; set; }
}