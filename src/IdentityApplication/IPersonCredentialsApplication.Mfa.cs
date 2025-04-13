using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

partial interface IPersonCredentialsApplication
{
    Task<Result<CredentialMfaAuthenticatorAssociation, Error>> AssociateMfaAuthenticatorAsync(
        ICallerContext caller, string? mfaToken, CredentialMfaAuthenticatorType authenticatorType,
        string? phoneNumber, CancellationToken cancellationToken);

    Task<Result<CredentialMfaAuthenticatorChallenge, Error>> ChallengeMfaAuthenticatorAsync(
        ICallerContext caller,
        string mfaToken, string authenticatorId, CancellationToken cancellationToken);

    Task<Result<PersonCredential, Error>> ChangeMfaAsync(ICallerContext caller, bool isEnabled,
        CancellationToken cancellationToken);

    Task<Result<CredentialMfaAuthenticatorConfirmation, Error>> ConfirmMfaAuthenticatorAssociationAsync(
        ICallerContext caller,
        string? mfaToken, CredentialMfaAuthenticatorType authenticatorType, string? oobCode,
        string confirmationCode, CancellationToken cancellationToken);

    Task<Result<Error>> DisassociateMfaAuthenticatorAsync(ICallerContext caller, string authenticatorId,
        CancellationToken cancellationToken);

    Task<Result<List<CredentialMfaAuthenticator>, Error>> ListMfaAuthenticatorsAsync(ICallerContext caller,
        string? mfaToken, CancellationToken cancellationToken);

    Task<Result<PersonCredential, Error>> ResetPasswordMfaAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> VerifyMfaAuthenticatorAsync(ICallerContext caller, string mfaToken,
        CredentialMfaAuthenticatorType authenticatorType, string? oobCode, string confirmationCode,
        CancellationToken cancellationToken);
}