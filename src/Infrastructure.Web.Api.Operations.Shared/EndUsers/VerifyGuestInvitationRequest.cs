using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/invitations/{Token}/verify", OperationMethod.Get)]
public class VerifyGuestInvitationRequest : UnTenantedRequest<VerifyGuestInvitationResponse>
{
    [Required] public string? Token { get; set; }
}