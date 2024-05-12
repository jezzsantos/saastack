using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     Verifies that the guest invitation is still valid
/// </summary>
[Route("/invitations/{Token}/verify", OperationMethod.Get)]
public class VerifyGuestInvitationRequest : UnTenantedRequest<VerifyGuestInvitationResponse>
{
    [Required] public string? Token { get; set; }
}