using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/register", ServiceOperation.Post)]
public class RegisterPersonPasswordRequest : UnTenantedRequest<RegisterPersonPasswordResponse>
{
    public string? CountryCode { get; set; }

    public required string EmailAddress { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Password { get; set; }

    public bool TermsAndConditionsAccepted { get; set; }

    public string? Timezone { get; set; }
}