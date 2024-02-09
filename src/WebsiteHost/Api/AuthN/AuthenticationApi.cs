using Application.Resources.Shared;
using Common;
using Common.Extensions;
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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationApi(ICallerContextFactory contextFactory, IAuthenticationApplication authenticationApplication,
        IHttpContextAccessor httpContextAccessor)
    {
        _contextFactory = contextFactory;
        _authenticationApplication = authenticationApplication;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _authenticationApplication.AuthenticateAsync(_contextFactory.Create(), request.Provider,
            request.AuthCode, request.Username, request.Password, cancellationToken);
        if (tokens.IsSuccessful)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            PopulateCookies(response, tokens.Value);
        }

        return () => tokens.HandleApplicationResult<AuthenticateResponse, AuthenticateTokens>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse { UserId = tok.UserId }));
    }

    public async Task<ApiEmptyResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationApplication.LogoutAsync(_contextFactory.Create(), cancellationToken);
        if (result.IsSuccessful)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            DeleteAuthenticationCookies(response);
        }

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenCookie(_httpContextAccessor.HttpContext!.Request);

        var tokens =
            await _authenticationApplication.RefreshTokenAsync(_contextFactory.Create(), refreshToken,
                cancellationToken);
        if (tokens.IsSuccessful)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            PopulateCookies(response, tokens.Value);
        }

        return () => tokens.Match(_ => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    private static void PopulateCookies(HttpResponse response, AuthenticateTokens tokens)
    {
        response.Cookies.Append(AuthenticationConstants.Cookies.Token, tokens.AccessToken,
            GetCookieOptions(tokens.ExpiresOn));
        response.Cookies.Append(AuthenticationConstants.Cookies.RefreshToken, tokens.RefreshToken, GetCookieOptions());
    }

    private static void DeleteAuthenticationCookies(HttpResponse response)
    {
        response.Cookies.Delete(AuthenticationConstants.Cookies.Token);
        response.Cookies.Delete(AuthenticationConstants.Cookies.RefreshToken);
    }

    private static Optional<string> GetRefreshTokenCookie(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.RefreshToken, out var cookie))
        {
            return cookie;
        }

        return Optional<string>.None;
    }

    private static CookieOptions GetCookieOptions(DateTime? expires = null)
    {
        var options = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
        if (expires.HasValue && expires.HasValue())
        {
            options.Expires = new DateTimeOffset(expires.Value);
        }

        return options;
    }
}