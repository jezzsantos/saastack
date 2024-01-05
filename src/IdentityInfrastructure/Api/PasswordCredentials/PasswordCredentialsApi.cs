using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using IdentityApplication;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class PasswordCredentialsApi : IWebApiService
{
    private readonly ICallerContext _context;
    private readonly IPasswordCredentialsApplication _passwordCredentialsApplication;

    public PasswordCredentialsApi(ICallerContext context,
        IPasswordCredentialsApplication passwordCredentialsApplication)
    {
        _context = context;
        _passwordCredentialsApplication = passwordCredentialsApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticatePasswordResponse>> Authenticate(
        AuthenticatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var authenticated =
            await _passwordCredentialsApplication.AuthenticateAsync(_context, request.Username, request.Password,
                cancellationToken);

        return () => authenticated.HandleApplicationResult<AuthenticatePasswordResponse, AuthenticateTokens>(x =>
            new PostResult<AuthenticatePasswordResponse>(new AuthenticatePasswordResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken,
                ExpiresOnUtc = x.ExpiresOn
            }));
    }

    public async Task<ApiEmptyResult> ConfirmRegistration(ConfirmPersonRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _passwordCredentialsApplication.ConfirmPersonRegistrationAsync(_context, request.Token,
                cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiPostResult<PasswordCredential, RegisterPersonResponse>> RegisterPerson(
        RegisterPersonRequest request,
        CancellationToken cancellationToken)
    {
        var credential = await _passwordCredentialsApplication.RegisterPersonAsync(_context, request.FirstName,
            request.LastName, request.EmailAddress, request.Password, request.Timezone, request.CountryCode,
            request.TermsAndConditionsAccepted, cancellationToken);

        return () => credential.HandleApplicationResult<RegisterPersonResponse, PasswordCredential>(x =>
            new PostResult<RegisterPersonResponse>(new RegisterPersonResponse { Credential = x }));
    }
}