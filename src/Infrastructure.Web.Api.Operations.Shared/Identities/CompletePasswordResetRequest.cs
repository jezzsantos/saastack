using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/{Token}/reset/complete", OperationMethod.Post)]
public class CompletePasswordResetRequest : UnTenantedEmptyRequest
{
    public required string Password { get; set; }

    public required string Token { get; set; }
}