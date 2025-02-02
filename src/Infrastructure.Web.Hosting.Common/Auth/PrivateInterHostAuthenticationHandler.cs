using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides a <see cref="IAuthenticationHandler" /> for private API authentication between hosts
/// </summary>
public class PrivateInterHostAuthenticationHandler : AuthenticationHandler<PrivateInterHostOptions>
{
    public const string AuthenticationScheme = "PrivateInterHost";

    public PrivateInterHostAuthenticationHandler(IOptionsMonitor<PrivateInterHostOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) :
        base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps)
        {
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_NotHttps);
        }

        var signature = Request.GetPrivateInterHostAuth();
        if (!signature.HasValue)
        {
            return AuthenticateResult.NoResult();
        }

        var caller = Context.RequestServices.GetRequiredService<ICallerContextFactory>().Create();
        var recorder = Context.RequestServices.GetRequiredService<IRecorder>();
        var hmacSecret = Context.RequestServices.GetRequiredService<IHostSettings>()
            .GetPrivateInterHostHmacAuthSecret();
        if (hmacSecret.HasNoValue())
        {
            recorder.TraceError(caller.ToCall(),
                Resources.PrivateInterHostAuthenticationHandler_Misconfigured_NoSecret);
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_Failed);
        }

        var isAuthenticated = await Request.VerifyHMACSignatureAsync(signature, hmacSecret, CancellationToken.None);
        if (!isAuthenticated)
        {
            recorder.Audit(caller.ToCall(), AuditingConstants.PrivateInterHostAuthenticationFailed,
                Resources.PrivateInterHostAuthenticationHandler_FailedAuthentication);
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_Failed);
        }

        return await DelegateToJwtBearerHandlerAsync(recorder, caller);
    }

    /// <summary>
    ///     We try to use the <see cref="JwtBearerHandler" /> to authenticate the attached token,
    ///     However, if there is no bearer token, we don't want to fail authentication altogether,
    ///     since we already authenticated the request, now we want to issue the token for the anonymous user
    /// </summary>
    private async Task<AuthenticateResult> DelegateToJwtBearerHandlerAsync(IRecorder recorder, ICallerContext caller)
    {
        var handlerProvider = Context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var jwtBearerHandler =
            await handlerProvider.GetHandlerAsync(Context, JwtBearerDefaults.AuthenticationScheme);
        if (jwtBearerHandler.NotExists())
        {
            recorder.TraceError(caller.ToCall(),
                Resources.PrivateInterHostAuthenticationHandler_Misconfigured_JwtProvider);
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_Failed);
        }

        var tokenResult = await jwtBearerHandler.AuthenticateAsync();
        if (!tokenResult.None)
        {
            return tokenResult;
        }

        return AuthenticateResult.Success(IssueTicket());

        AuthenticationTicket IssueTicket()
        {
            var authenticationScheme = $"{Scheme.Name};{JwtBearerDefaults.AuthenticationScheme}";
            var principal =
                new ClaimsPrincipal(
                    new ClaimsIdentity(ClaimExtensions.ToClaimsForAnonymousUser(), authenticationScheme));
            return new AuthenticationTicket(principal, authenticationScheme)
            {
                Properties =
                {
                    AllowRefresh = false,
                    IsPersistent = false
                }
            };
        }
    }
}

/// <summary>
///     Provides options for configuring private API authentication between hosts
/// </summary>
public class PrivateInterHostOptions : AuthenticationSchemeOptions;