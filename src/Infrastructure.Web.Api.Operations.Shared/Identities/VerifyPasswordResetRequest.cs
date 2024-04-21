using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/{Token}/reset/verify", OperationMethod.Get)]
public class VerifyPasswordResetRequest : UnTenantedEmptyRequest
{
    public required string Token { get; set; }
}