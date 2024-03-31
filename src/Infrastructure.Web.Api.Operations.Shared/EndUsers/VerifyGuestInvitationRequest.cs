using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/invitations/{Token}/verify", ServiceOperation.Get)]
public class VerifyGuestInvitationRequest : UnTenantedRequest<VerifyGuestInvitationResponse>
{
    public required string Token { get; set; }
}