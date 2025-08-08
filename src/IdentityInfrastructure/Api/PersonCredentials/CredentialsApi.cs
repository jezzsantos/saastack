using Application.Resources.Shared;
using Common;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PersonCredentials;

public class CredentialsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IPersonCredentialsApplication _personCredentialsApplication;

    public CredentialsApi(ICallerContextFactory callerFactory,
        IPersonCredentialsApplication personCredentialsApplication)
    {
        _callerFactory = callerFactory;
        _personCredentialsApplication = personCredentialsApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticateCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var authenticated =
            await _personCredentialsApplication.AuthenticateAsync(_callerFactory.Create(), request.Username!,
                request.Password!, cancellationToken);

        return () => authenticated.HandleApplicationResult<AuthenticateTokens, AuthenticateResponse>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse
            {
                Tokens = tok
            }));
    }

    public async Task<ApiEmptyResult> CompletePasswordReset(CompleteCredentialResetRequest request,
        CancellationToken cancellationToken)
    {
        var completion = await _personCredentialsApplication.CompletePasswordResetAsync(_callerFactory.Create(),
            request.Token!, request.Password!, cancellationToken);

        return () => completion.HandleApplicationResult();
    }

    public async Task<ApiEmptyResult> ConfirmRegistration(ConfirmRegistrationPersonCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _personCredentialsApplication.ConfirmPersonRegistrationAsync(_callerFactory.Create(),
                request.Token!, cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

#if TESTINGONLY
    public async Task<ApiGetResult<PersonCredentialEmailConfirmation, GetRegistrationPersonConfirmationResponse>>
        GetConfirmationToken(
            GetRegistrationPersonConfirmationRequest request, CancellationToken cancellationToken)
    {
        var token = await _personCredentialsApplication.GetPersonRegistrationConfirmationAsync(
            _callerFactory.Create(), request.UserId!, cancellationToken);

        return () =>
            token.HandleApplicationResult<PersonCredentialEmailConfirmation, GetRegistrationPersonConfirmationResponse>(
                con =>
                    new GetRegistrationPersonConfirmationResponse { Token = con.Token });
    }
#endif

    public async Task<ApiPostResult<PersonCredential, RegisterPersonCredentialResponse>> RegisterPerson(
        RegisterPersonCredentialRequest request, CancellationToken cancellationToken)
    {
        var credential = await _personCredentialsApplication.RegisterPersonAsync(_callerFactory.Create(),
            request.InvitationToken, request.FirstName!, request.LastName!, request.EmailAddress!, request.Password!,
            request.Timezone, request.Locale, request.CountryCode, request.TermsAndConditionsAccepted,
            cancellationToken);

        return () => credential.HandleApplicationResult<PersonCredential, RegisterPersonCredentialResponse>(creds =>
            new PostResult<RegisterPersonCredentialResponse>(new RegisterPersonCredentialResponse { Person = creds }));
    }

    public async Task<ApiEmptyResult> RequestPasswordReset(InitiatePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var reset = await _personCredentialsApplication.InitiatePasswordResetAsync(_callerFactory.Create(),
            request.EmailAddress!, cancellationToken);

        return () => reset.HandleApplicationResult();
    }

    public async Task<ApiEmptyResult> ResendPasswordReset(ResendPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var resent =
            await _personCredentialsApplication.ResendPasswordResetAsync(_callerFactory.Create(), request.Token!,
                cancellationToken);

        return () => resent.HandleApplicationResult();
    }

    public async Task<ApiEmptyResult> VerifyPasswordReset(VerifyPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var verified =
            await _personCredentialsApplication.VerifyPasswordResetAsync(_callerFactory.Create(), request.Token!,
                cancellationToken);

        return () => verified.HandleApplicationResult();
    }
}