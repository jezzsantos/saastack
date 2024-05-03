using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     Provides middleware to ensure that incoming requests have not been spoofed via CSRF attacks in the browser.
///     Implements several schemes, as per OWASP:
///     https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html
///     1. Double Submit Cookie, AND
///     2. Verifying Origin With Standard Headers
/// </summary>
public sealed class CSRFMiddleware
{
    private static readonly string[] IgnoredMethods =
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options
    };
    private readonly ICSRFService _csrfService;
    private readonly IHostSettings _hostSettings;
    private readonly RequestDelegate _next;
    private readonly IRecorder _recorder;

    public CSRFMiddleware(RequestDelegate next, IRecorder recorder, IHostSettings hostSettings,
        ICSRFService csrfService)
    {
        _next = next;
        _recorder = recorder;
        _hostSettings = hostSettings;
        _csrfService = csrfService;
    }

    public async Task InvokeAsync(HttpContext context, ICallerContextFactory callerContextFactory)
    {
        var caller = callerContextFactory.Create();
        var request = context.Request;
        if (IgnoredMethods.Contains(request.Method))
        {
            await _next(context); //Continue down the pipeline            
            return;
        }

        var result = VerifyRequest(request);
        if (result.IsFailure)
        {
            var httpError = result.Error.ToHttpError();
            var details = Results.Problem(statusCode: (int)httpError.Code, detail: httpError.Message);
            _recorder.Audit(caller.ToCall(), Audits.CSRFMiddleware_CSRFProtection_Failed,
                "User {Id} failed CSRF protection", caller.CallerId);
            await details
                .ExecuteAsync(context);
            return;
        }

        await _next(context); //Continue down the pipeline  
    }

    private Result<Error> VerifyRequest(HttpRequest request)
    {
        var hostName = GetHostName(_hostSettings);
        if (hostName.IsFailure)
        {
            return hostName.Error;
        }

        var csrfCookie = GetCookie(request);
        if (csrfCookie.IsFailure)
        {
            return csrfCookie.Error;
        }

        var csrfHeader = GetHeader(request);
        if (csrfHeader.IsFailure)
        {
            return csrfHeader.Error;
        }

        var userId = request.GetUserIdFromAuthNCookie();
        if (userId.IsFailure)
        {
            return userId.Error;
        }

        var verifiedCookie =
            VerifyCookieAndHeaderForUser(_recorder, _csrfService, csrfCookie.Value, csrfHeader.Value, userId.Value);
        if (verifiedCookie.IsFailure)
        {
            return verifiedCookie.Error;
        }

        var originHeader = GetHeader(request, HttpConstants.Headers.Origin);
        var refererHeader = GetHeader(request, HttpConstants.Headers.Referer);

        var verifiedOrigin = VerifyOrigin(_recorder, hostName.Value, originHeader, refererHeader);
        if (verifiedOrigin.IsFailure)
        {
            return verifiedOrigin.Error;
        }

        return Result.Ok;
    }

    private static Result<Error> VerifyOrigin(IRecorder recorder, string hostName, Optional<string> origin,
        Optional<string> referer)
    {
        if (!origin.HasValue && !referer.HasValue)
        {
            return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingOriginAndReferer);
        }

        if (origin.HasValue)
        {
            var originHost = GetHost(origin.Value);
            if (!originHost.HasValue || originHost.Value.NotEqualsIgnoreCase(hostName))
            {
                recorder.TraceError(null,
                    $"Request '{HttpConstants.Headers.Origin}' is not from a trusted site: '{{Origin}}'",
                    origin);
                return Error.ForbiddenAccess(Resources.CSRFMiddleware_OriginMismatched);
            }
        }

        if (referer.HasValue)
        {
            var refererHost = GetHost(referer.Value);
            if (!refererHost.HasValue || refererHost.Value.NotEqualsIgnoreCase(hostName))
            {
                recorder.TraceError(null,
                    $"Request '{HttpConstants.Headers.Referer}' is not from a trusted site: '{{Referer}}'",
                    origin);
                return Error.ForbiddenAccess(Resources.CSRFMiddleware_RefererMismatched);
            }
        }

        return Result.Ok;
    }

    private static Result<Error> VerifyCookieAndHeaderForUser(IRecorder recorder, ICSRFService csrfService,
        string csrfCookie, string csrfHeader, Optional<string> userId)
    {
        if (csrfCookie.HasNoValue() || csrfHeader.HasNoValue())
        {
            return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingCSRFCredentials);
        }

        var isVerified = csrfService.VerifyTokens(csrfHeader, csrfCookie, userId);
        if (isVerified)
        {
            return Result.Ok;
        }

        recorder.TraceError(null,
            "Request contains an invalid CSRF cookie signature for the CSRF token, and for the current user");
        return Error.ForbiddenAccess(Resources.CSRFMiddleware_InvalidSignature.Format(userId));
    }

    private static Result<string, Error> GetHeader(HttpRequest request)
    {
        var header = GetHeader(request, CSRFConstants.Headers.AntiCSRF);
        if (header.HasValue)
        {
            return header.Value;
        }

        return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingCSRFHeaderValue);
    }

    private static Optional<string> GetHeader(HttpRequest request, string name)
    {
        if (request.Headers.TryGetValue(name, out var value))
        {
            var values = value.ToString();

            return values.HasValue()
                ? values
                : Optional<string>.None;
        }

        return Optional<string>.None;
    }

    private static Result<string, Error> GetCookie(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(CSRFConstants.Cookies.AntiCSRF, out var value))
        {
            return value;
        }

        return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingCSRFCookieValue);
    }

    private static Result<string, Error> GetHostName(IHostSettings settings)
    {
        var baseUrl = settings.GetWebsiteHostBaseUrl();
        if (baseUrl.HasNoValue())
        {
            return Error.Unexpected(Resources.CSRFMiddleware_InvalidHostName.Format(baseUrl));
        }

        var hostName = GetHost(baseUrl);
        if (hostName.HasValue)
        {
            return hostName.Value;
        }

        return Error.Unexpected(Resources.CSRFMiddleware_InvalidHostName.Format(baseUrl));
    }

    private static Optional<string> GetHost(string name)
    {
        if (!Uri.TryCreate(name, UriKind.Absolute, out var uri))
        {
            return Optional<string>.None;
        }

        return uri.Host;
    }

    /// <summary>
    ///     Defines a service for creating and verifying CSRF token pairs
    /// </summary>
    public interface ICSRFService
    {
        CSRFTokenPair CreateTokens(Optional<string> userId);

        bool VerifyTokens(Optional<string> token, Optional<string> signature, Optional<string> userId);
    }
}