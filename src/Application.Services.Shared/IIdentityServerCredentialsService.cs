using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for managing credentials for an identity server
/// </summary>
public partial interface IIdentityServerCredentialsService
{
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string username, string password,
        CancellationToken cancellationToken);

    Task<Result<Error>> ConfirmPersonRegistrationAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);

    Task<Result<PersonCredential, Error>> GetPersonCredentialForUserAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

#if TESTINGONLY

    Task<Result<PersonCredentialEmailConfirmation, Error>> GetPersonRegistrationConfirmationForUserAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken);
#endif

    Task<Result<PersonCredential, Error>> RegisterPersonAsync(ICallerContext caller, string? invitationToken,
        string firstName, string lastName, string emailAddress, string password, string? timezone, string? locale,
        string? countryCode,
        bool termsAndConditionsAccepted, CancellationToken cancellationToken);
}

/// <summary>
///     Defines a service for changing credentials for an identity server
/// </summary>
public partial interface IIdentityServerCredentialsService
{
    Task<Result<Error>> CompletePasswordResetAsync(ICallerContext caller, string token, string password,
        CancellationToken cancellationToken);

    Task<Result<Error>> InitiatePasswordResetAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<Error>> ResendPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);

    Task<Result<Error>> VerifyPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);
}

/// <summary>
///     Defines a service for managing MFA for credentials for an identity server
/// </summary>
public partial interface IIdentityServerCredentialsService
{
    Task<Result<CredentialMfaAuthenticatorAssociation, Error>> AssociateMfaAuthenticatorForUserAsync(
        ICallerContext caller, string userId, string? mfaToken, CredentialMfaAuthenticatorType authenticatorType,
        string? phoneNumber, CancellationToken cancellationToken);

    Task<Result<CredentialMfaAuthenticatorChallenge, Error>> ChallengeMfaAuthenticatorForUserAsync(
        ICallerContext caller, string userId, string mfaToken, string authenticatorId,
        CancellationToken cancellationToken);

    Task<Result<PersonCredential, Error>> ChangeMfaForUserAsync(ICallerContext caller, string userId, bool isEnabled,
        CancellationToken cancellationToken);

    Task<Result<CredentialMfaAuthenticatorConfirmation, Error>> ConfirmMfaAuthenticatorAssociationForUserAsync(
        ICallerContext caller, string userId, string? mfaToken, CredentialMfaAuthenticatorType authenticatorType,
        string? oobCode, string confirmationCode,
        CancellationToken cancellationToken);

    Task<Result<Error>> DisassociateMfaAuthenticatorForUserAsync(ICallerContext caller, string userId,
        string authenticatorId, CancellationToken cancellationToken);

    Task<Result<List<CredentialMfaAuthenticator>, Error>> ListMfaAuthenticatorsForUserAsync(ICallerContext caller,
        string userId, string? mfaToken, CancellationToken cancellationToken);

    Task<Result<PersonCredential, Error>> ResetPasswordMfaForUserAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> VerifyMfaAuthenticatorForUserAsync(ICallerContext caller, string userId,
        string mfaToken,
        CredentialMfaAuthenticatorType authenticatorType, string? oobCode, string confirmationCode,
        CancellationToken cancellationToken);
}