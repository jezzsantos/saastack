using Application.Resources.Shared;
using Common;
using Common.Extensions;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class OAuth2Api : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IOpenIdConnectApplication _openIdConnectApplication;

    public OAuth2Api(ICallerContextFactory callerFactory, IOpenIdConnectApplication openIdConnectApplication)
    {
        _callerFactory = callerFactory;
        _openIdConnectApplication = openIdConnectApplication;
    }

    public async Task<ApiRedirectResult<OpenIdConnectAuthorization, EmptyResponse>> AuthorizeGet(
        AuthorizeOAuth2GetRequest request, CancellationToken cancellationToken)
    {
        var result = await _openIdConnectApplication.AuthorizeAsync(
            _callerFactory.Create(),
            request.ClientId!,
            request.RedirectUri!,
            request.ResponseType.ToEnumOrDefault(OAuth2ResponseType.Code),
            request.Scope!,
            request.State,
            request.Nonce,
            request.CodeChallenge,
            request.CodeChallengeMethod.ToEnumOrDefault(OpenIdConnectCodeChallengeMethod.Plain),
            cancellationToken);

        if (request.RedirectUri.HasNoValue())
        {
            return () => Error.Validation(Resources.OAuth2Api_AuthorizeGet_RedirectUriMIssing); //Should never get here
        }

        var redirectUri = result.Match(response =>
        {
            var code = response.Value.Code;
            if (code.Exists())
            {
                var codeParam = $"code={code.Code}";
                var stateParam = code.State.HasValue()
                    ? $"&state={code.State}"
                    : string.Empty;
                return $"{request.RedirectUri.WithoutTrailingSlash()}?{codeParam}{stateParam}";
            }

            return response.Value.RawRedirectUri;
        }, error =>
        {
            var errorCode = error.AdditionalCode.HasValue()
                ? error.AdditionalCode
                : error.Code.ToString();
            return $"{request.RedirectUri.WithoutTrailingSlash()}?error={errorCode}&error_description={error.Message}";
        });

        return () => new RedirectResult<EmptyResponse>(new EmptyResponse(), redirectUri);
    }

    public async Task<ApiRedirectResult<OpenIdConnectAuthorization, EmptyResponse>> AuthorizePost(
        AuthorizeOAuth2PostRequest request, CancellationToken cancellationToken)
    {
        var result = await _openIdConnectApplication.AuthorizeAsync(
            _callerFactory.Create(),
            request.ClientId!,
            request.RedirectUri!,
            request.ResponseType!.Value,
            request.Scope!,
            request.State,
            request.Nonce,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            cancellationToken);

        if (request.RedirectUri.HasNoValue())
        {
            return () => Error.Validation(Resources.OAuth2Api_AuthorizeGet_RedirectUriMIssing); //Should never get here
        }

        var redirectUri = result.Match(response =>
        {
            var code = response.Value.Code;
            if (code.Exists())
            {
                var codeParam = $"code={code.Code}";
                var stateParam = code.State.HasValue()
                    ? $"&state={code.State}"
                    : string.Empty;
                return $"{request.RedirectUri.WithoutTrailingSlash()}?{codeParam}{stateParam}";
            }

            return response.Value.RawRedirectUri;
        }, error =>
        {
            var errorCode = error.AdditionalCode.HasValue()
                ? error.AdditionalCode
                : error.Code.ToString();
            return $"{request.RedirectUri.WithoutTrailingSlash()}?error={errorCode}&error_description={error.Message}";
        });

        return () => new RedirectResult<EmptyResponse>(new EmptyResponse(), redirectUri);
    }

    public async Task<ApiPostResult<OpenIdConnectTokens, ExchangeOAuth2ForTokensResponse>> ExchangeCodeForTokens(
        ExchangeOAuth2ForTokensRequest request, CancellationToken cancellationToken)
    {
        var token = await _openIdConnectApplication.ExchangeCodeForTokensAsync(
            _callerFactory.Create(),
            request.GrantType!.Value,
            request.ClientId!,
            request.ClientSecret!,
            request.Code!,
            request.RedirectUri!,
            request.CodeVerifier,
            request.RefreshToken!, request.Scope, cancellationToken);

        return () => token.HandleApplicationResult<OpenIdConnectTokens, ExchangeOAuth2ForTokensResponse>(tok =>
            new PostResult<ExchangeOAuth2ForTokensResponse>(new ExchangeOAuth2ForTokensResponse
            {
                AccessToken = tok.AccessToken,
                TokenType = tok.TokenType,
                ExpiresIn = tok.ExpiresIn,
                RefreshToken = tok.RefreshToken,
                IdToken = tok.IdToken
            }));
    }

    public async Task<ApiGetResult<OpenIdConnectUserInfo, GetUserInfoForCallerResponse>> GetUserInfoForCaller(
        GetUserInfoForCallerRequest _, CancellationToken cancellationToken)
    {
        var user =
            await _openIdConnectApplication.GetUserInfoForCallerAsync(_callerFactory.Create(), cancellationToken);

        return () => user.HandleApplicationResult<OpenIdConnectUserInfo, GetUserInfoForCallerResponse>(usr =>
            new GetUserInfoForCallerResponse { User = usr });
    }
}