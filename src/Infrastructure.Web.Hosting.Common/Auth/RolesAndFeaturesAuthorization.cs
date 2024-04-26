using System.Collections.Concurrent;
using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using AuthorizeAttribute = Infrastructure.Web.Api.Interfaces.AuthorizeAttribute;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides an authorization policy provider for configuring policies related to roles and feature levels
/// </summary>
public sealed class RolesAndFeaturesAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private static readonly string[] ParticipatingAuthenticationSchemes =
    {
        JwtBearerDefaults.AuthenticationScheme,
        HMACAuthenticationHandler.AuthenticationScheme,
        APIKeyAuthenticationHandler.AuthenticationScheme
    };
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policyCache = new();

    public RolesAndFeaturesAuthorizationPolicyProvider(
        IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (_policyCache.TryGetValue(policyName, out var cachedPolicy))
        {
            return cachedPolicy;
        }

        // Search for this policy from the static list defined in HostExtensions
        var policy = await base.GetPolicyAsync(policyName);
        if (policy.Exists())
        {
            return policy;
        }

        var rolesAndFeatures = AuthorizeAttribute.ParsePolicyName(policyName);
        var requirements = rolesAndFeatures
            .Select(rf => new RolesAndFeaturesRequirement(rf.Roles, rf.Features))
            .Cast<IAuthorizationRequirement>().ToArray();

        var builder = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(ParticipatingAuthenticationSchemes)
            .RequireAuthenticatedUser()
            .AddRequirements(requirements)
            .Build();

        _policyCache.TryAdd(policyName, builder);

        return builder;
    }

#if TESTINGONLY
    internal bool IsCached(string policyName)
    {
        return _policyCache.TryGetValue(policyName, out _);
    }

    internal void CachePolicy(string policyName, AuthorizationPolicy builder)
    {
        _policyCache.TryAdd(policyName, builder);
    }
#endif
}

/// <summary>
///     Provides an authorization handler that processes an authorization requirement
/// </summary>
public sealed class RolesAndFeaturesAuthorizationHandler : AuthorizationHandler<RolesAndFeaturesRequirement>
{
    private readonly ICallerContextFactory _callerFactory;

    public RolesAndFeaturesAuthorizationHandler(ICallerContextFactory callerFactory)
    {
        _callerFactory = callerFactory;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RolesAndFeaturesRequirement requirement)
    {
        var caller = _callerFactory.Create();

        foreach (var platformRole in requirement.Roles.Platform)
        {
            if (!caller.Roles.Platform.ToList()
                    .Any(rol => rol == platformRole || rol.HasDescendant(platformRole)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingRole));
                return Task.CompletedTask;
            }
        }

        foreach (var tenantRole in requirement.Roles.Tenant)
        {
            if (!caller.Roles.Tenant.ToList()
                    .Any(rol => rol == tenantRole || rol.HasDescendant(tenantRole)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingRole));
                return Task.CompletedTask;
            }
        }

        foreach (var platformFeature in requirement.Features.Platform)
        {
            if (!caller.Features.Platform.ToList()
                    .Any(feat => feat == platformFeature || feat.HasDescendant(platformFeature)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingFeature));
                return Task.CompletedTask;
            }
        }

        foreach (var tenantFeature in requirement.Features.Tenant)
        {
            if (!caller.Features.Tenant.ToList()
                    .Any(feat => feat == tenantFeature || feat.HasDescendant(tenantFeature)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingFeature));
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
///     Provides an authorization requirement that will be asserted to authorize a request
/// </summary>
public sealed class RolesAndFeaturesRequirement : IAuthorizationRequirement
{
    public RolesAndFeaturesRequirement(ICallerContext.CallerRoles roles,
        ICallerContext.CallerFeatures features)
    {
        Roles = roles;
        Features = features;
    }

    public ICallerContext.CallerFeatures Features { get; }

    public ICallerContext.CallerRoles Roles { get; }
}