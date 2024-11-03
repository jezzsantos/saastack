using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

partial interface IPasswordCredentialsApplication
{
    Task<Result<PasswordCredentialMfaAuthenticatorAssociation, Error>> AssociateMfaAuthenticatorAsync(
        ICallerContext caller, string? mfaToken, PasswordCredentialMfaAuthenticatorType authenticatorType,
        string? phoneNumber, CancellationToken cancellationToken);

    Task<Result<PasswordCredentialMfaAuthenticatorChallenge, Error>> ChallengeMfaAuthenticatorAsync(
        ICallerContext caller,
        string mfaToken, string authenticatorId, CancellationToken cancellationToken);

    Task<Result<PasswordCredential, Error>> ChangeMfaAsync(ICallerContext caller, bool isEnabled,
        CancellationToken cancellationToken);

    Task<Result<PasswordCredentialMfaAuthenticatorConfirmation, Error>> ConfirmMfaAuthenticatorAssociationAsync(
        ICallerContext caller,
        string? mfaToken, PasswordCredentialMfaAuthenticatorType authenticatorType, string? oobCode,
        string confirmationCode, CancellationToken cancellationToken);

    Task<Result<Error>> DisassociateMfaAuthenticatorAsync(ICallerContext caller, string authenticatorId,
        CancellationToken cancellationToken);

    Task<Result<List<PasswordCredentialMfaAuthenticator>, Error>> ListMfaAuthenticatorsAsync(ICallerContext caller,
        string? mfaToken, CancellationToken cancellationToken);

    Task<Result<PasswordCredential, Error>> ResetPasswordMfaAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> VerifyMfaAuthenticatorAsync(ICallerContext caller, string mfaToken,
        PasswordCredentialMfaAuthenticatorType authenticatorType, string? oobCode, string confirmationCode,
        CancellationToken cancellationToken);
}