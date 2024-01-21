using System.Security.Claims;
using Application.Common;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     A <see cref="ICallerContext" /> that reads the context from ASP.NET
/// </summary>
internal sealed class AspNetCallerContext : ICallerContext
{
    public AspNetCallerContext(IHttpContextAccessor httpContext)
    {
        var context = httpContext.HttpContext!;
        var claims = context.User.Claims.ToArray();
        TenantId = GetTenantId(context);
        CallId = context.Items.TryGetValue(RequestCorrelationFilter.CorrelationIdItemName,
            out var callId)
            ? callId!.ToString()!
            : Caller.GenerateCallId();
        CallerId = GetCallerId(claims);
        IsServiceAccount = CallerConstants.IsServiceAccount(CallerId);
        Roles = GetRoles(claims, TenantId);
        Features = GetFeatures(claims, TenantId);
        Authorization = GetAuthorization(context);
        IsAuthenticated = IsServiceAccount
                          || (Authorization.HasValue && !CallerConstants.IsAnonymousUser(CallerId));
    }

    public string CallerId { get; }

    public string CallId { get; }

    public string? TenantId { get; }

    public ICallerContext.CallerRoles Roles { get; }

    public ICallerContext.CallerFeatures Features { get; }

    public Optional<ICallerContext.CallerAuthorization> Authorization { get; }

    public bool IsAuthenticated { get; }

    public bool IsServiceAccount { get; }

    // ReSharper disable once ReturnTypeCanBeNotNullable
    // ReSharper disable once UnusedParameter.Local
    private static string? GetTenantId(HttpContext context)
    {
        //HACK: if the request does not come in with an OrganizationId, then no tenant possible
        return MultiTenancyConstants
            .DefaultOrganizationId; //HACK: until we finish multi-tenancy , and fetch this from context.Items
    }

    private static ICallerContext.CallerAuthorization GetAuthorization(HttpContext context)
    {
        var authenticationFeature = context.Features.Get<IAuthenticateResultFeature>();
        if (!authenticationFeature.Exists())
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        var scheme = authenticationFeature.AuthenticateResult?.Ticket?.AuthenticationScheme;
        if (scheme.NotExists())
        {
            return Optional<ICallerContext.CallerAuthorization>.None;
        }

        return GetCallerAuthorization(context, scheme);
    }

    private static ICallerContext.CallerAuthorization GetCallerAuthorization(HttpContext context, string scheme)
    {
        if (scheme == JwtBearerDefaults.AuthenticationScheme)
        {
            var token = context.Request.GetTokenAuth();
            if (!token.HasValue)
            {
                return Optional<ICallerContext.CallerAuthorization>.None;
            }

            return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.Token, token);
        }

        if (scheme == APIKeyAuthenticationHandler.AuthenticationScheme)
        {
            var apikey = context.Request.GetAPIKeyAuth();
            if (!apikey.HasValue)
            {
                return Optional<ICallerContext.CallerAuthorization>.None;
            }

            return new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.APIKey, apikey);
        }

        if (scheme == HMACAuthenticationHandler.AuthenticationScheme)
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