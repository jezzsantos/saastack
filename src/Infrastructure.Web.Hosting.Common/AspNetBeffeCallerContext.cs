using System.Security.Claims;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides <see cref="ICallerContext" /> that reads the context of the caller from a
///     <see cref="AuthenticationConstants.Cookies.Token" /> cookie in
///     ASPNET. Assumes that neither Authentication nor Authorization have been configured on this host.
/// </summary>
internal sealed class AspNetBeffeCallerContext : AspNetClaimsBasedCallerContextBase
{
    public AspNetBeffeCallerContext(IHostSettings hostSettings,
        IHttpContextAccessor httpContextAccessor) : base(hostSettings,
        GetClaimsFromAuthNCookie(httpContextAccessor),
        GetAuthorizationFromAuthNCookie(httpContextAccessor),
        Optional<string>.None, GetCorrelationId(httpContextAccessor))
    {
    }

    private static Claim[] GetClaimsFromAuthNCookie(IHttpContextAccessor contextAccessor)
    {
        var httpContext = contextAccessor.HttpContext;
        if (httpContext.NotExists())
        {
            return [];
        }

        return httpContext.Request.GetClaimsFromAuthNCookie();
    }

    private static ICallerContext.CallerAuthorization GetAuthorizationFromAuthNCookie(
        IHttpContextAccessor contextAccessor)
    {
        var httpContext = contextAccessor.HttpContext;
        if (httpContext.NotExists())
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        var token = httpContext.Request.TokenFromAuthNCookie();
        if (!token.HasValue)
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.Token, token);
    }
}