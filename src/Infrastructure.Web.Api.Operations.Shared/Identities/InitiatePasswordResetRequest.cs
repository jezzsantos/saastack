using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/reset", OperationMethod.Post)]
public class InitiatePasswordResetRequest : UnTenantedEmptyRequest
{
    public required string EmailAddress { get; set; }
}