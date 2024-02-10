using Application.Resources.Shared;
using Common;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class PasswordCredentialsApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IPasswordCredentialsApplication _passwordCredentialsApplication;

    public PasswordCredentialsApi(ICallerContextFactory contextFactory,
        IPasswordCredentialsApplication passwordCredentialsApplication)
    {
        _contextFactory = contextFactory;
        _passwordCredentialsApplication = passwordCredentialsApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var authenticated =
            await _passwordCredentialsApplication.AuthenticateAsync(_contextFactory.Create(), request.Username,
                request.Password,
                cancellationToken);

        return () => authenticated.HandleApplicationResult<AuthenticateResponse, AuthenticateTokens>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse
            {
                AccessToken = tok.AccessToken,
                RefreshToken = tok.RefreshToken,
                AccessTokenExpiresOnUtc = tok.AccessTokenExpiresOn,
                RefreshTokenExpiresOnUtc = tok.RefreshTokenExpiresOn,
                UserId = tok.UserId
            }));
    }

    public async Task<ApiEmptyResult> ConfirmRegistration(ConfirmRegistrationPersonPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _passwordCredentialsApplication.ConfirmPersonRegistrationAsync(_contextFactory.Create(),
                request.Token,
                cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

#if TESTINGONLY
    public async Task<ApiGetResult<PasswordCredentialConfirmation, GetRegistrationPersonConfirmationResponse>>
        GetConfirmationToken(
            GetRegistrationPersonConfirmationRequest request, CancellationToken cancellationToken)
    {
        var token = await _passwordCredentialsApplication.GetPersonRegistrationConfirmationAsync(
            _contextFactory.Create(),
            request.UserId, cancellationToken);

        return () =>
            token.HandleApplicationResult<GetRegistrationPersonConfirmationResponse, PasswordCredentialConfirmation>(
                con =>
                    new GetRegistrationPersonConfirmationResponse { Token = con.Token });
    }
#endif

    public async Task<ApiPostResult<PasswordCredential, RegisterPersonPasswordResponse>> RegisterPerson(
        RegisterPersonPasswordRequest request, CancellationToken cancellationToken)
    {
        var credential = await _passwordCredentialsApplication.RegisterPersonAsync(_contextFactory.Create(),
            request.FirstName,
            request.LastName, request.EmailAddress, request.Password, request.Timezone, request.CountryCode,
            request.TermsAndConditionsAccepted, cancellationToken);

        return () => credential.HandleApplicationResult<RegisterPersonPasswordResponse, PasswordCredential>(creds =>
            new PostResult<RegisterPersonPasswordResponse>(new RegisterPersonPasswordResponse { Credential = creds }));
    }
}