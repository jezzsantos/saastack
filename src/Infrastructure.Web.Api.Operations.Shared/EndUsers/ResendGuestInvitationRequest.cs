using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     Resends the invitation to the specified person to the platform
/// </summary>
[Route("/invitations/{Token}/resend", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class ResendGuestInvitationRequest : UnTenantedEmptyRequest<ResendGuestInvitationRequest>
{
    [Required] public string? Token { get; set; }
}