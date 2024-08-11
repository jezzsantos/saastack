using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

/// <summary>
///     Changes the user's avatar image
/// </summary>
[Route("/profiles/{UserId}/avatar", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class ChangeProfileAvatarRequest : UnTenantedRequest<ChangeProfileAvatarRequest, ChangeProfileAvatarResponse>,
    IHasMultipartForm
{
    // Will also include bytes for the multipart-form image
    [Required] public string? UserId { get; set; }
}