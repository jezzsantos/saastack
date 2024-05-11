using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

[Route("/invitations", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class InviteGuestRequest : UnTenantedRequest<InviteGuestResponse>
{
    [Required] public string? Email { get; set; }
}