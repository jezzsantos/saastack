using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

[Route("/profiles/{UserId}/avatar", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class DeleteProfileAvatarRequest : UnTenantedRequest<DeleteProfileAvatarResponse>
{
    public required string UserId { get; set; }
}