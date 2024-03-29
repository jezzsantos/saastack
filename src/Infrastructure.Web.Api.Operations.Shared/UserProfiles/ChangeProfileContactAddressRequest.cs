using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

[Route("/profiles/{UserId}/contact", ServiceOperation.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class ChangeProfileContactAddressRequest : UnTenantedRequest<GetProfileResponse>
{
    public string? City { get; set; }

    public string? CountryCode { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? State { get; set; }

    public required string UserId { get; set; }

    public string? Zip { get; set; }
}