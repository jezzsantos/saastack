using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Resets the user MFA status back to the default for all users
/// </summary>
[Route("/credentials/mfa/reset", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class ResetCredentialMfaRequest : UnTenantedRequest<ResetCredentialMfaRequest, ChangeCredentialMfaResponse>
{
    public string? UserId { get; set; }
}