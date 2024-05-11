#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/confirm-registration", OperationMethod.Get, isTestingOnly: true)]
public class GetRegistrationPersonConfirmationRequest : UnTenantedRequest<GetRegistrationPersonConfirmationResponse>
{
    public required string UserId { get; set; }
}
#endif