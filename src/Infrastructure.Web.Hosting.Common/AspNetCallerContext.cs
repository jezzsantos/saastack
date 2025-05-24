using System.Security.Claims;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Services;
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
        var tenancyContext = httpContext.RequestServices.GetService<ITenancyContext>()!;
        var regionService = httpContext.RequestServices.GetService<IHostSettings>()!;
        TenantId = GetTenantId(tenancyContext);
        CallId = httpContext.Items.TryGetValue(RequestCorrelationFilter.CorrelationIdItemName,
            out var callId)
            ? callId!.ToString()!
            : Caller.GenerateCallId();
        var claims = httpContext.User.Claims.ToArray();
        CallerId = GetCallerId(claims);
        IsServiceAccount = CallerConstants.IsServiceAccount(CallerId);
        Roles = GetRoles(claims, TenantId);
        Features = GetFeatures(claims, TenantId);
        Authorization = GetAuthorization(httpContext);
        IsAuthenticated = IsServiceAccount
                          || (Authorization.HasValue && !CallerConstants.IsAnonymousUser(CallerId));
        HostRegion = regionService.GetRegion();
    }

    public Optional<ICallerContext.CallerAuthorization> Authorization { get; }

    public string CallerId { get; }

    public string CallId { get; }

    public ICallerContext.CallerFeatures Features { get; }

    public DatacenterLocation HostRegion { get; }

    public bool IsAuthenticated { get; }

    public bool IsServiceAccount { get; }

    public ICallerContext.CallerRoles Roles { get; }

    public Optional<string> TenantId { get; }

    private static string? GetTenantId(ITenancyContext? tenancyContext)
    {
        return tenancyContext.Exists()
            ? tenancyContext.Current
            : null;
    }

    private static Optional<ICallerContext.CallerAuthorization> GetAuthorization(HttpContext context)
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

    private static Optional<ICallerContext.CallerAuthorization> GetCallerAuthorization(HttpContext context,
        List<string> schemes)
    {
        // This scheme check needs to come before JwtBearer check, since PrivateInterHost will always include JwtBearer also 
        if (schemes.ContainsIgnoreCase(PrivateInterHostAuthenticationHandler.AuthenticationScheme))
        {
            var token = context.Request.GetTokenAuth();
            return !token.HasValue
                ? new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.PrivateInterHost,
                    Optional<string>.None)
                : new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.PrivateInterHost, token);
        }

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
                return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.APIKey,
                    Optional<string>.None);
            }

            return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.APIKey, apikey);
        }

        if (schemes.ContainsIgnoreCase(HMACAuthenticationHandler.AuthenticationScheme))
        {
            return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.HMAC,
                Optional<string>.None);
        }

        return Optional<ICallerContext.CallerAuthorization>.None;
    }

    /// <summary>
    ///     Get the claim identifying the ID of the user.
    ///     Note: Can also be the <see cref="CallerConstants.AnonymousUserId" /> user too!
    /// </summary>
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