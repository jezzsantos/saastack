using System.Text;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Extensions;
using Infrastructure.Web.Interfaces;
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
    internal const string CSRFViolation = "csrf_violation";
    private static readonly string[] IgnoredMethods =
    [
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options
    ];
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
            var details = result.Error.ToProblem();
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

        var csrfCookie = GetCSRFCookie(request);
        if (csrfCookie.IsFailure)
        {
            return csrfCookie.Error;
        }

        var csrfHeader = GetCSRFHeader(request);
        if (csrfHeader.IsFailure)
        {
            return csrfHeader.Error;
        }

        var currentUserId = request.GetUserIdFromAuthNCookie();
        if (currentUserId.IsFailure)
        {
            return Error.ForbiddenAccess(Resources.CSRFMiddleware_InvalidAuthCookie, CSRFViolation);
        }

        var verifiedCookie =
            VerifyCookieAndHeaderForUser(_recorder, _csrfService, csrfCookie.Value, csrfHeader.Value,
                currentUserId.Value);
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
            return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingOriginAndReferer, CSRFViolation);
        }

        if (origin.HasValue)
        {
            var originHost = GetHost(origin.Value);
            if (!originHost.HasValue || originHost.Value.NotEqualsIgnoreCase(hostName))
            {
                recorder.TraceError(null,
                    $"Request '{HttpConstants.Headers.Origin}' is not from a trusted site: '{{Origin}}'",
                    origin);
                return Error.ForbiddenAccess(Resources.CSRFMiddleware_OriginMismatched, CSRFViolation);
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
                return Error.ForbiddenAccess(Resources.CSRFMiddleware_RefererMismatched, CSRFViolation);
            }
        }

        return Result.Ok;
    }

    /// <summary>
    ///     Verifies the CSRF header and cookie values, for the authenticated/unauthenticated user
    ///     Note: It is possible that the current authenticated user (from the auth-tok), by now, has expired (and
    ///     the cookie has disappeared),
    ///     so, we need to fall back to the last user id from when the CSRF cookie was created.
    /// </summary>
    private static Result<Error> VerifyCookieAndHeaderForUser(IRecorder recorder, ICSRFService csrfService,
        CSRFCookie csrfCookie, string csrfHeader, Optional<string> authenticatedUserId)
    {
        if (csrfCookie.Signature.HasNoValue()
            || csrfHeader.HasNoValue())
        {
            return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingCSRFCredentials, CSRFViolation);
        }

        var userId = authenticatedUserId.HasValue
            ? authenticatedUserId.Value.ToOptional()
            : csrfCookie.LastUserId.ToOptional();

        var isVerified = csrfService.VerifyTokens(csrfHeader, csrfCookie.Signature, userId);
        if (isVerified)
        {
            return Result.Ok;
        }

        recorder.TraceError(null,
            "Request contains an invalid CSRF cookie signature for the user {UserId}",
            userId.ValueOrNull ?? "unauthenticated");
        return Error.ForbiddenAccess(Resources.CSRFMiddleware_InvalidSignature, CSRFViolation);
    }

    private static Result<string, Error> GetCSRFHeader(HttpRequest request)
    {
        var header = GetHeader(request, CSRFConstants.Headers.AntiCSRF);
        if (header.HasValue)
        {
            return header.Value;
        }

        return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingCSRFHeaderValue, CSRFViolation);
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

    private static Result<CSRFCookie, Error> GetCSRFCookie(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(CSRFConstants.Cookies.AntiCSRF, out var value))
        {
            var signatureData = CSRFCookie.FromCookieValue(value);
            if (signatureData.IsFailure)
            {
                return signatureData.Error;
            }

            return signatureData;
        }

        return Error.ForbiddenAccess(Resources.CSRFMiddleware_MissingCSRFCookieValue, CSRFViolation);
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

    /// <summary>
    ///     Defines the CSRF signature data
    /// </summary>
    public record CSRFCookie(string? LastUserId, string Signature)
    {
        /// <summary>
        ///     Converts the specified <see cref="cookieValue" /> to a new instance
        /// </summary>
        public static Result<CSRFCookie, Error> FromCookieValue(string cookieValue)
        {
            try
            {
                return Encoding.UTF8.GetString(
                        Convert.FromBase64String(cookieValue))
                    .FromJson<CSRFCookie>()!;
            }
            catch (Exception ex)
            {
                return ex.ToError(ErrorCode.Unexpected);
            }
        }

        /// <summary>
        ///     Converts this instance to a value that can be inserted into a request or response cookie
        ///     Note: Request cookies (in particular) cannot contain either a semicolon or a comma,
        ///     which is why we have to base64 encode this JSON value
        /// </summary>
        public string ToCookieValue()
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(new CSRFCookie(
                    LastUserId.Exists()
                        ? LastUserId
                        : null, Signature).ToJson(false)!));
        }
    }
}