using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IdentityApplication;

public partial class PersonCredentialsApplication : IPersonCredentialsApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public PersonCredentialsApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string username,
        string password, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.AuthenticateAsync(caller, username, password,
            cancellationToken);
    }

    public async Task<Result<Error>> ConfirmPersonRegistrationAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ConfirmPersonRegistrationAsync(caller, token,
            cancellationToken);
    }

#if TESTINGONLY
    public async Task<Result<PersonCredentialEmailConfirmation, Error>> GetPersonRegistrationConfirmationAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.GetPersonRegistrationConfirmationForUserAsync(caller,
            userId,
            cancellationToken);
    }
#endif

    public async Task<Result<PersonCredential, Error>> RegisterPersonAsync(ICallerContext caller,
        string? invitationToken, string firstName, string lastName, string emailAddress, string password,
        string? timezone, string? countryCode, bool termsAndConditionsAccepted, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.RegisterPersonAsync(caller, invitationToken, firstName,
            lastName, emailAddress, password, timezone, countryCode, termsAndConditionsAccepted, cancellationToken);
    }
}