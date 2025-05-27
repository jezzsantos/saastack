using System.Security.Claims;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common.Extensions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides <see cref="ICallerContext" /> that reads the context of the caller from the <see cref="HttpContext" /> in
///     ASPNET. Assumes that both Authentication and Authorization have both been configured on this host.
/// </summary>
internal sealed class AspNetHttpContextCallerContext : AspNetClaimsBasedCallerContextBase
{
    public AspNetHttpContextCallerContext(ITenancyContext tenancyContext, IHostSettings hostSettings,
        IHttpContextAccessor httpContextAccessor) : base(hostSettings,
        GetClaimsFromPrincipal(httpContextAccessor),
        GetAuthorization(httpContextAccessor),
        GetTenantId(tenancyContext),
        GetCorrelationId(httpContextAccessor))
    {
    }

    private static Claim[] GetClaimsFromPrincipal(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext.NotExists())
        {
            return [];
        }

        return httpContext.User.Claims.ToArray();
    }
}