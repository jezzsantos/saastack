using System.ComponentModel.DataAnnotations;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Verifies an MFA authenticator for the user, and authenticates the user.
/// </summary>
/// <remarks>
///     This API can only be called Anonymously (during password authentication)
/// </remarks>
[Route("/passwords/mfa/authenticators/{AuthenticatorType}/verify", OperationMethod.PutPatch)]
public class
    VerifyPasswordMfaAuthenticatorForCallerRequest : UnTenantedRequest<VerifyPasswordMfaAuthenticatorForCallerRequest,
    AuthenticateResponse>
{
    [Required] public PasswordCredentialMfaAuthenticatorType? AuthenticatorType { get; set; }

    [Required] public string? ConfirmationCode { get; set; }

    [Required] public string? MfaToken { get; set; }

    public string? OobCode { get; set; }
}