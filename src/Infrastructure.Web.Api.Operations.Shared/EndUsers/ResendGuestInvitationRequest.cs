using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/invitations/{Token}/resend", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class ResendGuestInvitationRequest : UnTenantedEmptyRequest
{
    [Required] public string? Token { get; set; }
}