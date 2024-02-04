using Application.Resources.Shared;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using WebsiteHost.Application;

namespace WebsiteHost.Api.AuthN;

public class AuthenticationApi : IWebApiService
{
    private readonly IAuthenticationApplication _authenticationApplication;
    private readonly ICallerContextFactory _contextFactory;

    public AuthenticationApi(ICallerContextFactory contextFactory, IAuthenticationApplication authenticationApplication)
    {
        _contextFactory = contextFactory;
        _authenticationApplication = authenticationApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _authenticationApplication.AuthenticateAsync(_contextFactory.Create(), request.Provider,
            request.AuthCode, request.Username, request.Password, cancellationToken);

        return () => tokens.HandleApplicationResult<AuthenticateResponse, AuthenticateTokens>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse { UserId = tok.UserId }));
    }

#pragma warning disable SAS014
    public async Task<ApiEmptyResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
#pragma warning restore SAS014
    {
        var result = await _authenticationApplication.LogoutAsync(_contextFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationApplication.RefreshTokenAsync(_contextFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
}