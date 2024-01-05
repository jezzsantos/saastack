using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/confirm-registration", ServiceOperation.Post)]
public class ConfirmPersonRegistrationRequest : UnTenantedRequest<ConfirmPersonRegistrationResponse>
{
    public required string Token { get; set; }
}