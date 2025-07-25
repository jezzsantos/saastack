using Application.Resources.Shared;
using IdentityApplication;
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
        var response = await _openIdConnectApplication.AuthorizeAsync(
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

        return () => response.HandleApplicationResult<OidcAuthorizationResponse, AuthorizeResponse>(auth =>
            new AuthorizeResponse
            {
                Code = auth.Code,
                State = auth.State
            });
    }

    public async Task<ApiPostResult<OidcTokenResponse, TokenEndpointResponse>> GetToken(
        TokenEndpointRequest request, CancellationToken cancellationToken)
    {
        var token = await _openIdConnectApplication.CreateTokenAsync(
            _callerFactory.Create(),
            request.GrantType!,
            request.ClientId!,
            request.ClientSecret!,
            request.Code!,
            request.CodeVerifier,
            request.RedirectUri!,
            request.RefreshToken!,
            request.Scope, cancellationToken);

        return () => token.HandleApplicationResult<OidcTokenResponse, TokenEndpointResponse>(t =>
            new PostResult<TokenEndpointResponse>(new TokenEndpointResponse
            {
                AccessToken = t.AccessToken,
                TokenType = t.TokenType,
                ExpiresIn = t.ExpiresIn,
                RefreshToken = t.RefreshToken,
                IdToken = t.IdToken,
                Scope = t.Scope
            }));
    }

    public async Task<ApiGetResult<OidcUserInfoResponse, GetUserInfoForCallerResponse>> GetUserInfoForCaller(
        GetUserInfoForCallerRequest _, CancellationToken cancellationToken)
    {
        var userInfo =
            await _openIdConnectApplication.GetUserInfoForCallerAsync(_callerFactory.Create(), cancellationToken);

        return () => userInfo.HandleApplicationResult<OidcUserInfoResponse, GetUserInfoForCallerResponse>(ui =>
            new GetUserInfoForCallerResponse { OidcUserInfo = ui });
    }
}