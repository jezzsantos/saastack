using Application.Resources.Shared;
using Common;
using Common.Extensions;
using IdentityApplication.ApplicationServices;
using IdentityDomain;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OpenIdConnect;

public class OAuth2Api : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IOpenIdConnectApplication _openIdConnectApplication;

    public OAuth2Api(ICallerContextFactory callerFactory, IOpenIdConnectApplication openIdConnectApplication)
    {
        _callerFactory = callerFactory;
        _openIdConnectApplication = openIdConnectApplication;
    }

    public async Task<ApiGetResult<OidcAuthorizationResponse, AuthorizeResponse>> Authorize(
        AuthorizeRequest request, CancellationToken cancellationToken)
    {
        var result = await _openIdConnectApplication.AuthorizeAsync(
            _callerFactory.Create(),
            request.ClientId!,
            request.RedirectUri!,
            request.ResponseType!,
            request.Scope!,
            request.State,
            request.Nonce,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            cancellationToken);

        return () => result.HandleApplicationResult<OidcAuthorizationResponse, AuthorizeResponse>(auth =>
            new AuthorizeResponse
            {
                Code = auth.Code,
                State = auth.State
            });
    }

    public async Task<ApiPostResult<OidcTokenResponse, TokenEndpointResponse>> GetToken(
        TokenEndpointRequest request, CancellationToken cancellationToken)
    {
        Result<OidcTokenResponse, Error> result;

        if (request.GrantType.EqualsIgnoreCase(OpenIdConnectConstants.GrantTypes.AuthorizationCode))
        {
            result = await _openIdConnectApplication.ExchangeCodeForTokensAsync(
                _callerFactory.Create(),
                request.ClientId!,
                request.ClientSecret!,
                request.Code!,
                request.RedirectUri!,
                request.CodeVerifier,
                cancellationToken);
        }
        else if (request.GrantType.EqualsIgnoreCase(OpenIdConnectConstants.GrantTypes.RefreshToken))
        {
            result = await _openIdConnectApplication.RefreshTokenAsync(
                _callerFactory.Create(),
                request.ClientId!,
                request.ClientSecret!,
                request.RefreshToken!,
                request.Scope,
                cancellationToken);
        }
        else
        {
            return () => new Result<PostResult<TokenEndpointResponse>, Error>(
                Error.Validation($"Unsupported grant type: {request.GrantType}"));
        }

        return () => result.HandleApplicationResult<OidcTokenResponse, TokenEndpointResponse>(token =>
            new PostResult<TokenEndpointResponse>(new TokenEndpointResponse
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType,
                ExpiresIn = token.ExpiresIn,
                RefreshToken = token.RefreshToken,
                IdToken = token.IdToken,
                Scope = token.Scope
            }));
    }

    public async Task<ApiGetResult<OidcUserInfoResponse, GetUserInfoForCallerResponse>> GetUserInfoForCaller(
        GetUserInfoForCallerRequest _, CancellationToken cancellationToken)
    {
        var result =
            await _openIdConnectApplication.GetUserInfoForCallerAsync(_callerFactory.Create(), cancellationToken);

        return () => result.HandleApplicationResult<OidcUserInfoResponse, GetUserInfoForCallerResponse>(userInfo =>
            new GetUserInfoForCallerResponse { OidcUserInfo = userInfo });
    }
}