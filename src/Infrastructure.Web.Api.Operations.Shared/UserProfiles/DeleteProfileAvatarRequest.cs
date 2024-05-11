using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

/// <summary>
///     Removes the user's avatar image
/// </summary>
[Route("/profiles/{UserId}/avatar", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class DeleteProfileAvatarRequest : UnTenantedRequest<DeleteProfileAvatarResponse>
{
    [Required] public string? UserId { get; set; }
}