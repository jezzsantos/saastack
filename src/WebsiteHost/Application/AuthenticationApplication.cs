using Application.Common;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;

namespace WebsiteHost.Application;

public class AuthenticationApplication : IAuthenticationApplication
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public AuthenticationApplication(IRecorder recorder, IHttpContextAccessor httpContextAccessor,
        IServiceClient serviceClient)
    {
        _recorder = recorder;
        _httpContextAccessor = httpContextAccessor;
        _serviceClient = serviceClient;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext context, string provider,
        string? authCode, string? username, string? password, CancellationToken cancellationToken)
    {
        Task<Result<AuthenticateResponse, ResponseProblem>> request;
        switch (provider)
        {
            case AuthenticationConstants.Providers.Credentials:
                request = _serviceClient.PostAsync(context, new AuthenticatePasswordRequest
                {
                    Username = username!,
                    Password = password!
                }, null, cancellationToken);
                break;

            default:
                request = _serviceClient.PostAsync(context, new AuthenticateSingleSignOnRequest
                {
                    AuthCode = authCode!,
                    Provider = provider
                }, null, cancellationToken);
                break;
        }

        var authenticated = await request;
        if (!authenticated.IsSuccessful)
        {
            return authenticated.Error.ToError();
        }

        var tokens = authenticated.Value.Convert<AuthenticateResponse, AuthenticateTokens>();
        var response = _httpContextAccessor.HttpContext!.Response;
        PopulateCookies(response, tokens);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.UserLogin);

        return new Result<AuthenticateTokens, Error>(tokens);
    }

    public Task<Result<Error>> LogoutAsync(ICallerContext context, CancellationToken cancellationToken)
    {
        var response = _httpContextAccessor.HttpContext!.Response;
        DeleteAuthenticationCookies(response);

        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.UserLogout);

        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<Error>> RefreshTokenAsync(ICallerContext context, CancellationToken cancellationToken)
    {
        var request = _httpContextAccessor.HttpContext!.Request;
        var refreshToken = GetRefreshTokenCookie(request);
        if (!refreshToken.HasValue)
        {
            return Error.EntityNotFound();
        }

        var refreshed = await _serviceClient.PostAsync(context, new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        }, null, cancellationToken);
        if (!refreshed.IsSuccessful)
        {
            return refreshed.Error.ToError();
        }

        var tokens = refreshed.Value.Convert<RefreshTokenResponse, AuthenticateTokens>();
        var response = _httpContextAccessor.HttpContext!.Response;
        PopulateCookies(response, tokens);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.UserExtendedLogin);

        return Result.Ok;
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