#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the confirmation token for a registration of a person
/// </summary>
[Route("/passwords/confirm-registration", OperationMethod.Get, isTestingOnly: true)]
public class GetRegistrationPersonConfirmationRequest : UnTenantedRequest<GetRegistrationPersonConfirmationRequest,
    GetRegistrationPersonConfirmationResponse>
{
    [Required] public string? UserId { get; set; }
}
#endif