using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Registers a new person on the platform
/// </summary>
[Route("/passwords/register", OperationMethod.Post)]
public class RegisterPersonPasswordRequest : UnTenantedRequest<RegisterPersonPasswordResponse>
{
    public string? CountryCode { get; set; }

    [Required] public string? EmailAddress { get; set; }

    [Required] public string? FirstName { get; set; }

    public string? InvitationToken { get; set; }

    [Required] public string? LastName { get; set; }

    [Required] public string? Password { get; set; }

    public bool TermsAndConditionsAccepted { get; set; }

    public string? Timezone { get; set; }
}