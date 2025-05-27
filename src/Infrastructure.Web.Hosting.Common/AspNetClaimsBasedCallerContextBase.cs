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

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides <see cref="ICallerContext" /> that reads the context of the caller from the claims in ASPNET.
/// </summary>
public abstract class AspNetClaimsBasedCallerContextBase : ICallerContext
{
    protected AspNetClaimsBasedCallerContextBase(IHostSettings hostSettings,
        IReadOnlyList<Claim> claims, Optional<ICallerContext.CallerAuthorization> authorization, string tenantId,
        string correlationId)
    {
        TenantId = tenantId;
        CallId = correlationId;
        CallerId = GetCallerId(claims);
        Roles = GetRoles(claims, TenantId);
        Features = GetFeatures(claims, TenantId);
        IsServiceAccount = CallerConstants.IsServiceAccount(CallerId);
        var isAnonymous = CallerConstants.IsAnonymousUser(CallerId);
        Authorization = authorization.HasValue && !isAnonymous
            ? authorization
            : Optional<ICallerContext.CallerAuthorization>.None;
        IsAuthenticated = IsServiceAccount
                          || (Authorization.HasValue && !isAnonymous);
        HostRegion = hostSettings.GetRegion();
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

    /// <summary>
    ///     Extracts the tenant ID from the tenancy context.
    /// </summary>
    public static Optional<string> GetTenantId(ITenancyContext? tenancyContext)
    {
        return tenancyContext.Exists()
            ? tenancyContext.Current
            : null;
    }

    /// <summary>
    ///     Returns authorization details from ASPNET Authentication adn Authorization configuration
    /// </summary>
    protected static Optional<ICallerContext.CallerAuthorization> GetAuthorization(IHttpContextAccessor contextAccessor)
    {
        var httpContext = contextAccessor.HttpContext;
        if (httpContext.NotExists())
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        var authenticationFeature = httpContext.Features.Get<IAuthenticateResultFeature>();
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

        return GetCallerAuthorization(httpContext, schemes.ToList());
    }

    /// <summary>
    ///     Retrieves the correlation ID from the items collection in the request pipeline,
    ///     that should have been set by the <see cref="RequestCorrelationFilter" />.
    /// </summary>
    protected static string GetCorrelationId(IHttpContextAccessor contextAccessor)
    {
        var httpContext = contextAccessor.HttpContext;
        if (httpContext.NotExists())
        {
            return Caller.GenerateCallId();
        }

        return httpContext.Items.TryGetValue(RequestCorrelationFilter.CorrelationIdItemName,
            out var callId)
            ? callId!.ToString()!
            : Caller.GenerateCallId();
    }

    /// <summary>
    ///     Extracts the authorization details from the request, based on the available authentication schemes
    /// </summary>
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
    private static string GetCallerId(IReadOnlyList<Claim> claims)
    {
        var userClaim = claims.FirstOrDefault(claim => claim.Type == AuthenticationConstants.Claims.ForId);
        if (userClaim.Exists())
        {
            return userClaim.Value;
        }

        return CallerConstants.AnonymousUserId;
    }

    private static ICallerContext.CallerFeatures GetFeatures(IReadOnlyList<Claim> claims, string? tenantId)
    {
        var platformFeatures = claims.ToArray().GetPlatformFeatures();
        var tenantFeatures = claims.ToArray().GetTenantFeatures(tenantId);

        return new ICallerContext.CallerFeatures(platformFeatures, tenantFeatures);
    }

    private static ICallerContext.CallerRoles GetRoles(IReadOnlyList<Claim> claims, string? tenantId)
    {
        var platformRoles = claims.ToArray().GetPlatformRoles();
        var tenantRoles = claims.ToArray().GetTenantRoles(tenantId);

        return new ICallerContext.CallerRoles(platformRoles, tenantRoles);
    }
}