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

        return () => authenticated.HandleApplicationResult<AuthenticateTokens, AuthenticateResponse>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse
            {
                Tokens = tok
            }));
    }

    public async Task<ApiEmptyResult> CompletePasswordReset(CompletePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var completion = await _passwordCredentialsApplication.CompletePasswordResetAsync(_contextFactory.Create(),
            request.Token, request.Password, cancellationToken);

        return () => completion.HandleApplicationResult();
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
            token.HandleApplicationResult<PasswordCredentialConfirmation, GetRegistrationPersonConfirmationResponse>(
                con =>
                    new GetRegistrationPersonConfirmationResponse { Token = con.Token });
    }
#endif

    public async Task<ApiPostResult<PasswordCredential, RegisterPersonPasswordResponse>> RegisterPerson(
        RegisterPersonPasswordRequest request, CancellationToken cancellationToken)
    {
        var credential = await _passwordCredentialsApplication.RegisterPersonAsync(_contextFactory.Create(),
            request.InvitationToken,
            request.FirstName, request.LastName, request.EmailAddress, request.Password, request.Timezone,
            request.CountryCode, request.TermsAndConditionsAccepted, cancellationToken);

        return () => credential.HandleApplicationResult<PasswordCredential, RegisterPersonPasswordResponse>(creds =>
            new PostResult<RegisterPersonPasswordResponse>(new RegisterPersonPasswordResponse { Credential = creds }));
    }

    public async Task<ApiEmptyResult> RequestPasswordReset(InitiatePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var reset = await _passwordCredentialsApplication.InitiatePasswordResetAsync(_contextFactory.Create(),
            request.EmailAddress, cancellationToken);

        return () => reset.HandleApplicationResult();
    }

    public async Task<ApiEmptyResult> ResendPasswordReset(ResendPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var resent =
            await _passwordCredentialsApplication.ResendPasswordResetAsync(_contextFactory.Create(), request.Token,
                cancellationToken);

        return () => resent.HandleApplicationResult();
    }

    public async Task<ApiEmptyResult> VerifyPasswordReset(VerifyPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var verified =
            await _passwordCredentialsApplication.VerifyPasswordResetAsync(_contextFactory.Create(), request.Token,
                cancellationToken);

        return () => verified.HandleApplicationResult();
    }
}