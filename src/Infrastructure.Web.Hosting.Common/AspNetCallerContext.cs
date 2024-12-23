using System.Security.Claims;
using Application.Common;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     A <see cref="ICallerContext" /> that reads the context from ASP.NET
/// </summary>
internal sealed class AspNetCallerContext : ICallerContext
{
    public AspNetCallerContext(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var claims = httpContext.User.Claims.ToArray();
        var tenancyContext = httpContext.RequestServices.GetService<ITenancyContext>();
        TenantId = GetTenantId(tenancyContext);
        CallId = httpContext.Items.TryGetValue(RequestCorrelationFilter.CorrelationIdItemName,
            out var callId)
            ? callId!.ToString()!
            : Caller.GenerateCallId();
        CallerId = GetCallerId(claims);
        IsServiceAccount = CallerConstants.IsServiceAccount(CallerId);
        Roles = GetRoles(claims, TenantId);
        Features = GetFeatures(claims, TenantId);
        Authorization = GetAuthorization(httpContext);
        IsAuthenticated = IsServiceAccount
                          || (Authorization.HasValue && !CallerConstants.IsAnonymousUser(CallerId));
    }

    public Optional<ICallerContext.CallerAuthorization> Authorization { get; }

    public string CallerId { get; }

    public string CallId { get; }

    public ICallerContext.CallerFeatures Features { get; }

    public bool IsAuthenticated { get; }

    public bool IsServiceAccount { get; }

    public ICallerContext.CallerRoles Roles { get; }

    public string? TenantId { get; }

    private static string? GetTenantId(ITenancyContext? tenancyContext)
    {
        return tenancyContext.Exists()
            ? tenancyContext.Current
            : null;
    }

    private static ICallerContext.CallerAuthorization GetAuthorization(HttpContext context)
    {
        var authenticationFeature = context.Features.Get<IAuthenticateResultFeature>();
        if (authenticationFeature.NotExists())
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        var scheme = authenticationFeature.AuthenticateResult?.Ticket?.AuthenticationScheme;
        var schemes = scheme.HasValue()
            ? scheme.Split(',', ';')
            : [];
        if (schemes.HasNone())
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        return GetCallerAuthorization(context, schemes.ToList());
    }

    private static ICallerContext.CallerAuthorization GetCallerAuthorization(HttpContext context, List<string> schemes)
    {
        if (schemes.ContainsIgnoreCase(JwtBearerDefaults.AuthenticationScheme))
        {
            var token = context.Request.GetTokenAuth();
            if (!token.HasValue)
            {
                return Optional<ICallerContext.CallerAuthorization>.None;
            }

            return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.Token, token);
        }

        if (schemes.ContainsIgnoreCase(APIKeyAuthenticationHandler.AuthenticationScheme))
        {
            var apikey = context.Request.GetAPIKeyAuth();
            if (!apikey.HasValue)
            {
                return Optional<ICallerContext.CallerAuthorization>.None;
            }

            return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.APIKey, apikey);
        }

        if (schemes.ContainsIgnoreCase(HMACAuthenticationHandler.AuthenticationScheme))
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        return Optional<ICallerContext.CallerAuthorization>.None;
    }

    private static string GetCallerId(Claim[] claims)
    {
        var userClaim = claims.FirstOrDefault(claim => claim.Type == AuthenticationConstants.Claims.ForId);
        if (userClaim.Exists())
        {
            return userClaim.Value;
        }

        return CallerConstants.AnonymousUserId;
    }

    private static ICallerContext.CallerFeatures GetFeatures(Claim[] claims, string? tenantId)
    {
        var platformFeatures = claims.GetPlatformFeatures();
        var tenantFeatures = claims.GetTenantFeatures(tenantId);

        return new ICallerContext.CallerFeatures(platformFeatures, tenantFeatures);
    }

    private static ICallerContext.CallerRoles GetRoles(Claim[] claims, string? tenantId)
    {
        var platformRoles = claims.GetPlatformRoles();
        var tenantRoles = claims.GetTenantRoles(tenantId);

        return new ICallerContext.CallerRoles(platformRoles, tenantRoles);
    }
}