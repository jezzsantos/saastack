#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/confirm-registration", OperationMethod.Get, isTestingOnly: true)]
public class GetRegistrationPersonConfirmationRequest : UnTenantedRequest<GetRegistrationPersonConfirmationResponse>
{
    [Required] public string? UserId { get; set; }
}
#endif