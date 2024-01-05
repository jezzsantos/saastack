using System.Security.Claims;
using Application.Common;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
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
        CallId = context.Items.TryGetValue(RequestCorrelationFilter.CorrelationIdItemName,
            out var callId)
            ? callId!.ToString()!
            : Caller.GenerateCallId();
        CallerId = GetCallerId(claims);
        IsServiceAccount = CallerConstants.IsServiceAccount(CallerId);
        Roles = GetRoles(claims);
        FeatureLevels = GetFeatureLevels(claims);
        Authorization = GetAuthorization(context);
        IsAuthenticated = IsServiceAccount
                          || (Authorization.HasValue && !CallerConstants.IsAnonymousUser(CallerId));
    }

    public string CallerId { get; }

    public string CallId { get; }

    public string? TenantId => null;

    public ICallerContext.CallerRoles Roles { get; }

    public ICallerContext.CallerFeatureLevels FeatureLevels { get; }

    public Optional<ICallerContext.CallerAuthorization> Authorization { get; }

    public bool IsAuthenticated { get; }

    public bool IsServiceAccount { get; }

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
        var userClaim = claims.FirstOrDefault(claim => claim.Type == AuthenticationConstants.ClaimForId);
        if (userClaim.Exists())
        {
            return userClaim.Value;
        }

        return CallerConstants.AnonymousUserId;
    }

    private static ICallerContext.CallerFeatureLevels GetFeatureLevels(IEnumerable<Claim> claims)
    {
        return new ICallerContext.CallerFeatureLevels(claims
            .Where(claim => claim.Type == AuthenticationConstants.ClaimForFeatureLevel)
            .Select(claim => new FeatureLevel(claim.Value))
            .ToArray(), Array.Empty<FeatureLevel>());
    }

    private static ICallerContext.CallerRoles GetRoles(IEnumerable<Claim> claims)
    {
        return new ICallerContext.CallerRoles(claims
            .Where(claim => claim.Type == AuthenticationConstants.ClaimForRole)
            .Select(claim => claim.Value)
            .ToArray(), Array.Empty<string>());
    }
}