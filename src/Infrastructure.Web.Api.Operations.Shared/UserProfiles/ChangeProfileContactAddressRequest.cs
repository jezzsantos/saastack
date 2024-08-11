using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

/// <summary>
///     Changes the user's contact address
/// </summary>
[Route("/profiles/{UserId}/contact", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class
    ChangeProfileContactAddressRequest : UnTenantedRequest<ChangeProfileContactAddressRequest, GetProfileResponse>
{
    public string? City { get; set; }

    public string? CountryCode { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? State { get; set; }

    [Required] public string? UserId { get; set; }

    public string? Zip { get; set; }
}