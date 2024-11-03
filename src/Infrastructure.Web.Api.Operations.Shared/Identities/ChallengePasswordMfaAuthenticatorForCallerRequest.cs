using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Challenges an MFA authenticator for the user.
///     Depending on the specific authenticator, this can send an SMS or email to the user containing a secret code.
/// </summary>
/// <remarks>
///     This API can only be called Anonymously (during password authentication)
/// </remarks>
[Route("/passwords/mfa/authenticators/{AuthenticatorId}/challenge", OperationMethod.PutPatch)]
public class ChallengePasswordMfaAuthenticatorForCallerRequest : UnTenantedRequest<
    ChallengePasswordMfaAuthenticatorForCallerRequest, ChallengePasswordMfaAuthenticatorForCallerResponse>
{
    [Required] public string? AuthenticatorId { get; set; }

    [Required] public string? MfaToken { get; set; }
}