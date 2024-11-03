using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Resets the user MFA status back to the default for all users
/// </summary>
[Route("/passwords/mfa/reset", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Operations, Features.Platform_PaidTrial)]
public class ResetPasswordMfaRequest : UnTenantedRequest<ResetPasswordMfaRequest, ChangePasswordMfaResponse>
{
    public string? UserId { get; set; }
}