using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IPasswordCredentialsApplication
{
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string username, string password,
        CancellationToken cancellationToken);

    Task<Result<Error>> CompletePasswordResetAsync(ICallerContext caller, string token, string password,
        CancellationToken cancellationToken);

    Task<Result<Error>> ConfirmPersonRegistrationAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<PasswordCredentialConfirmation, Error>> GetPersonRegistrationConfirmationAsync(ICallerContext caller,
        string userId, CancellationToken cancellationToken);
#endif

    Task<Result<Error>> InitiatePasswordResetAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<PasswordCredential, Error>> RegisterPersonAsync(ICallerContext caller, string? invitationToken,
        string firstName,
        string lastName, string emailAddress, string password, string? timezone, string? countryCode,
        bool termsAndConditionsAccepted, CancellationToken cancellationToken);

    Task<Result<Error>> ResendPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);

    Task<Result<Error>> VerifyPasswordResetAsync(ICallerContext caller, string token, CancellationToken
        cancellationToken);
}