using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/confirm-registration", OperationMethod.Post)]
public class ConfirmRegistrationPersonPasswordRequest : UnTenantedRequest<ConfirmRegistrationPersonPasswordResponse>
{
    public required string Token { get; set; }
}