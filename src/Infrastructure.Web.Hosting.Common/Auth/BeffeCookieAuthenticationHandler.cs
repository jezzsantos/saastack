using System.Security.Claims;
using System.Text.Encodings.Web;
using Common.Extensions;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides a <see cref="IAuthenticationHandler" /> for BEFFE AuthN Cookie authentication.
///     Note this handler does not assert any AuthZ policies, as we are not enforcing them on the BEFFE.
/// </summary>
public class BeffeCookieAuthenticationHandler : AuthenticationHandler<BeffeCookieOptions>
{
    public const string AuthenticationScheme = "AuthNCookie";

    public BeffeCookieAuthenticationHandler(IOptionsMonitor<BeffeCookieOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Task.CompletedTask;
        if (!Request.IsHttps)
        {
            return AuthenticateResult.Fail(Resources.AuthenticationHandler_NotHttps);
        }

        var claims = Request.GetClaimsFromAuthNCookie();
        if (!claims.HasAny())
        {
            return AuthenticateResult.NoResult();
        }

        return AuthenticateResult.Success(IssueTicket());

        AuthenticationTicket IssueTicket()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
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
///     Provides options for configuring BEFFE AuthN Cookie authentication
/// </summary>
public class BeffeCookieOptions : AuthenticationSchemeOptions;