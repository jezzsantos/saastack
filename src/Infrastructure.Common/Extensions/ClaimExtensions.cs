using System.Security.Claims;
using Application.Resources.Shared;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Infrastructure.Interfaces;

namespace Infrastructure.Common.Extensions;

public static class ClaimExtensions
{
    private const string TenantIdDelimiter = "##";

    /// <summary>
    ///     Returns the set of <see cref="FeatureLevel" /> found in the specified <see cref="claims" />
    /// </summary>
    public static FeatureLevel[] GetPlatformFeatures(this Claim[] claims)
    {
        return claims
            .Where(claim => claim.Type == AuthenticationConstants.Claims.ForFeature)
            .Select<Claim, FeatureLevel?>(
                claim => FindPlatformFeatureByClaimValue(claim.Value))
            .Where(feature => feature is not null)!
            .ToArray<FeatureLevel>();
    }

    /// <summary>
    ///     Returns the set of <see cref="RoleLevel" /> found in the specified <see cref="claims" />
    /// </summary>
    public static RoleLevel[] GetPlatformRoles(this Claim[] claims)
    {
        return claims
            .Where(claim => claim.Type == AuthenticationConstants.Claims.ForRole)
            .Select<Claim, RoleLevel?>(claim => FindPlatformRoleByClaimValue(claim.Value))
            .Where(role => role is not null)!
            .ToArray<RoleLevel>();
    }

    /// <summary>
    ///     Returns the set of <see cref="FeatureLevel" /> found in the specified <see cref="claims" />
    /// </summary>
    public static FeatureLevel[] GetTenantFeatures(this Claim[] claims, string? tenantId)
    {
        return claims
            .Where(claim => claim.Type == AuthenticationConstants.Claims.ForFeature)
            .Select<Claim, FeatureLevel?>(
                claim => FindTenantFeatureByClaimValue(claim.Value, tenantId))
            .Where(feature => feature is not null)!
            .ToArray<FeatureLevel>();
    }

    /// <summary>
    ///     Returns the set of <see cref="RoleLevel" /> found in the specified <see cref="claims" />
    /// </summary>
    public static RoleLevel[] GetTenantRoles(this Claim[] claims, string? tenantId)
    {
        return claims
            .Where(claim => claim.Type == AuthenticationConstants.Claims.ForRole)
            .Select<Claim, RoleLevel?>(
                claim => FindTenantRoleByClaimValue(claim.Value, tenantId))
            .Where(role => role is not null)!
            .ToArray<RoleLevel>();
    }

    /// <summary>
    ///     Returns the claims for the specified <see cref="user" />
    /// </summary>
    public static IReadOnlyList<Claim> ToClaims(this EndUserWithMemberships user)
    {
        var claims = new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForId, user.Id)
        };
        user.Roles
            .ForEach(rol =>
            {
                var role = PlatformRoles.FindRoleByName(rol);
                if (role is not null)
                {
                    claims.Add(new Claim(AuthenticationConstants.Claims.ForRole, ToPlatformClaimValue(role)));
                }
            });
        user.Features
            .ForEach(feat =>
            {
                var feature = PlatformFeatures.FindFeatureByName(feat);
                if (feature is not null)
                {
                    claims.Add(new Claim(AuthenticationConstants.Claims.ForFeature,
                        ToPlatformClaimValue(feature)));
                }
            });

        user.Memberships.ForEach(membership =>
        {
            membership.Roles
                .ForEach(rol =>
                {
                    var role = TenantRoles.FindRoleByName(rol);
                    if (role is not null)
                    {
                        claims.Add(new Claim(AuthenticationConstants.Claims.ForRole,
                            ToTenantClaimValue(role, membership.OrganizationId)));
                    }
                });
            membership.Features
                .ForEach(feat =>
                {
                    var feature = TenantFeatures.FindFeatureByName(feat);
                    if (feature is not null)
                    {
                        claims.Add(new Claim(AuthenticationConstants.Claims.ForFeature,
                            ToTenantClaimValue(feature, membership.OrganizationId)));
                    }
                });
        });

        return claims;
    }

    /// <summary>
    ///     Returns the claims for a service account
    /// </summary>
    public static IReadOnlyList<Claim> ToClaimsForServiceAccount()
    {
        return new[]
        {
            new Claim(AuthenticationConstants.Claims.ForId, CallerConstants.MaintenanceAccountUserId),
            new Claim(AuthenticationConstants.Claims.ForRole,
                ToPlatformClaimValue(PlatformRoles.ServiceAccount)),
            new Claim(AuthenticationConstants.Claims.ForFeature, ToPlatformClaimValue(PlatformFeatures.Basic))
        };
    }

    /// <summary>
    ///     Returns the name of this feature for use in claims
    /// </summary>
    public static string ToPlatformClaimValue(FeatureLevel feature)
    {
        return $"{AuthenticationConstants.Claims.PlatformPrefix}_{feature.Name}";
    }

    /// <summary>
    ///     Returns the name of this role for use in claims
    /// </summary>
    public static string ToPlatformClaimValue(RoleLevel role)
    {
        return $"{AuthenticationConstants.Claims.PlatformPrefix}_{role.Name}";
    }

    /// <summary>
    ///     Returns the name of this feature for use in claims
    ///     Example format: Tenant_standard::atenantid
    /// </summary>
    public static string ToTenantClaimValue(FeatureLevel feature, string tenantId)
    {
        return $"{AuthenticationConstants.Claims.TenantPrefix}_{feature.Name}{TenantIdDelimiter}{tenantId}";
    }

    /// <summary>
    ///     Returns the name of this role for use in claims.
    ///     Example format: Tenant_standard::atenantid
    /// </summary>
    public static string ToTenantClaimValue(RoleLevel role, string tenantId)
    {
        return $"{AuthenticationConstants.Claims.TenantPrefix}_{role.Name}{TenantIdDelimiter}{tenantId}";
    }

    private static FeatureLevel? FindPlatformFeatureByClaimValue(string value)
    {
        const string token = $"{AuthenticationConstants.Claims.PlatformPrefix}_";
        var indexOfName = value.IndexOf(token, StringComparison.Ordinal);
        if (indexOfName == -1)
        {
            return null;
        }

        var featureName = value.Substring(indexOfName + token.Length);
#if NETSTANDARD2_0
        return AllFeatures.TryGetValue(featureName, out var feature)
            ? feature
            : null;
#else
        return PlatformFeatures.AllFeatures.GetValueOrDefault(featureName);
#endif
    }

    private static RoleLevel? FindPlatformRoleByClaimValue(string value)
    {
        const string token = $"{AuthenticationConstants.Claims.PlatformPrefix}_";
        var indexOfName = value.IndexOf(token, StringComparison.Ordinal);
        if (indexOfName == -1)
        {
            return null;
        }

        var roleName = value.Substring(indexOfName + token.Length);
#if NETSTANDARD2_0
        return AllRoles.TryGetValue(roleName, out var role)
            ? role
            : null;
#else
        return PlatformRoles.AllRoles.GetValueOrDefault(roleName);
#endif
    }

    private static FeatureLevel? FindTenantFeatureByClaimValue(string value, string? tenantId)
    {
        if (!ParseTenantClaimValue(value, tenantId, out var featureName))
        {
            return null;
        }
#if NETSTANDARD2_0
        return AllFeatures.TryGetValue(featureName, out var feature)
            ? feature
            : null;
#else
        return TenantFeatures.AllFeatures.GetValueOrDefault(featureName);
#endif
    }

    private static RoleLevel? FindTenantRoleByClaimValue(string value, string? tenantId)
    {
        if (!ParseTenantClaimValue(value, tenantId, out var roleName))
        {
            return null;
        }
#if NETSTANDARD2_0
        return AllRoles.TryGetValue(roleName, out var role)
            ? role
            : null;
#else
        return TenantRoles.AllRoles.GetValueOrDefault(roleName);
#endif
    }

    private static bool ParseTenantClaimValue(string value, string? tenantId, out string result)
    {
        if (tenantId.HasNoValue())
        {
            result = value;
            return false;
        }

        const string token = $"{AuthenticationConstants.Claims.TenantPrefix}_";
        var indexOfToken = value.IndexOf(token, StringComparison.Ordinal);
        if (indexOfToken == -1)
        {
            result = value;
            return false;
        }

        var indexOfTenantSeparator = value.IndexOf(TenantIdDelimiter, StringComparison.Ordinal);
        if (indexOfTenantSeparator == -1)
        {
            result = value;
            return false;
        }

        var extractedTenantId = value.Substring(indexOfTenantSeparator + TenantIdDelimiter.Length);
        if (extractedTenantId.NotEqualsOrdinal(tenantId))
        {
            result = value;
            return false;
        }

        var level = value.Substring(indexOfToken + token.Length, indexOfTenantSeparator - token.Length);
        result = level;
        return true;
    }
}