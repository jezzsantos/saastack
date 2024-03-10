using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common.Extensions;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides a <see cref="IAuthenticationHandler" /> for APIKey authentication
/// </summary>
public class APIKeyAuthenticationHandler : AuthenticationHandler<APIKeyOptions>
{
    public const string AuthenticationScheme = "APIKey";

    public APIKeyAuthenticationHandler(IOptionsMonitor<APIKeyOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps)
        {
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_NotHttps);
        }

        var apiKey = Request.GetAPIKeyAuth();
        if (!apiKey.HasValue)
        {
            return AuthenticateResult.Fail(
                Resources.APIKeyAuthenticationHandler_MissingParameter.Format(HttpQueryParams.APIKey));
        }

        var caller = Context.RequestServices.GetRequiredService<ICallerContextFactory>().Create();
        var identityService = Context.RequestServices.GetRequiredService<IIdentityService>();
        var lookup = await identityService.FindMembershipsForAPIKeyAsync(caller, apiKey, CancellationToken.None);
        if (!lookup.IsSuccessful)
        {
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_Failed);
        }

        var isAuthenticated = lookup.Value.HasValue;
        if (!isAuthenticated)
        {
            var recorder = Context.RequestServices.GetRequiredService<IRecorder>();
            recorder.Audit(caller.ToCall(), AuditingConstants.APIKeyAuthenticationFailed,
                Resources.APIKeyAuthenticationHandler_FailedAuthentication);
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_Failed);
        }

        var user = lookup.Value.Value;
        return AuthenticateResult.Success(IssueTicket());

        AuthenticationTicket IssueTicket()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(user.ToClaims(), Scheme.Name));
            return new AuthenticationTicket(principal, Scheme.Name)
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
///     Provides options for configuring APIKey authentication
/// </summary>
public class APIKeyOptions : AuthenticationSchemeOptions;