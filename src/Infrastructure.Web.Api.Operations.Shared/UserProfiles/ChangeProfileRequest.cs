using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

/// <summary>
///     Changes the user's profile information
/// </summary>
[Route("/profiles/{UserId}", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class ChangeProfileRequest : UnTenantedRequest<GetProfileResponse>
{
    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Timezone { get; set; }

    [Required] public string? UserId { get; set; }
}