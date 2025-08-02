using System.Security.Claims;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Resources.Shared.Extensions;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;

namespace Infrastructure.Common.Extensions;

public static class ClaimExtensions
{
    public const string TenantIdDelimiter = "#|#";

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
    public static IReadOnlyList<Claim> ToClaims(this EndUserWithMemberships user,
        Dictionary<string, object>? additionalData)
    {
        var additionalClaims = additionalData ?? new Dictionary<string, object>();
        var now = DateTime.UtcNow.ToNearestSecond();
        var claims = new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForId, user.Id),
            new(AuthenticationConstants.Claims.ForIssuedAt, new DateTimeOffset(now).ToUnixTimeSeconds().ToString())
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

        // Add at_hash for implicit flow,
        // see https://openid.net/specs/openid-connect-core-1_0.html#TokenSubstitution
        if (additionalClaims.TryGetValue(AuthenticationConstants.Claims.ForAtHash, out var value2))
        {
            if (value2.ToString().HasValue())
            {
                var atHash = value2.ToString()!;
                claims.Add(new Claim(AuthenticationConstants.Claims.ForAtHash, atHash));
            }
        }

        // Add c_hash for hybrid flow,
        // see https://openid.net/specs/openid-connect-core-1_0.html#CodeValidation
        if (additionalClaims.TryGetValue(AuthenticationConstants.Claims.ForCHash, out var value3))
        {
            if (value3.ToString().HasValue())
            {
                var nonce = value3.ToString()!;
                claims.Add(new Claim(AuthenticationConstants.Claims.ForCHash, nonce));
            }
        }

        return claims;
    }

    /// <summary>
    ///     Returns the claims for the specified <see cref="profile" />
    /// </summary>
    public static IReadOnlyList<Claim> ToClaims(this UserProfile profile, IReadOnlyList<string>? scopes,
        Dictionary<string, object>? additionalData)
    {
        var additionalClaims = additionalData ?? new Dictionary<string, object>();
        var authScopes = scopes ?? new List<string>();
        var now = DateTime.UtcNow.ToNearestSecond();
        var claims = new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForId, profile.UserId),
            new(AuthenticationConstants.Claims.ForIssuedAt, new DateTimeOffset(now).ToUnixTimeSeconds().ToString())
        };

        if (authScopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Email))
        {
            claims.Add(new Claim(AuthenticationConstants.Claims.ForGivenName, profile.Name.FirstName));
            claims.Add(new Claim(AuthenticationConstants.Claims.ForFamilyName, profile.Name.LastName ?? ""));
            claims.Add(new Claim(AuthenticationConstants.Claims.ForFullName, profile.Name.FullName()));
            claims.Add(new Claim(AuthenticationConstants.Claims.ForNickName, profile.DisplayName));

            if (profile.PhoneNumber.HasValue())
            {
                claims.Add(new Claim(AuthenticationConstants.Claims.ForPhoneNumber, profile.PhoneNumber));
            }

            if (profile.Timezone.HasValue())
            {
                claims.Add(new Claim(AuthenticationConstants.Claims.ForTimezone, profile.Timezone));
            }

            if (profile.AvatarUrl.HasValue())
            {
                claims.Add(new Claim(AuthenticationConstants.Claims.ForPicture, profile.AvatarUrl));
            }
        }

        if (authScopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Email))
        {
            if (profile.EmailAddress.HasValue())
            {
                claims.Add(new Claim(AuthenticationConstants.Claims.ForEmail, profile.EmailAddress));
                claims.Add(new Claim(AuthenticationConstants.Claims.ForEmailVerified, true.ToString()));
            }
        }

        // Add nonce for replay attack prevention,
        // see https://openid.net/specs/openid-connect-core-1_0.html#NonceNotes
        if (additionalClaims.TryGetValue(AuthenticationConstants.Claims.ForNonce, out var value1))
        {
            if (value1.ToString().HasValue())
            {
                var nonce = value1.ToString()!;
                claims.Add(new Claim(AuthenticationConstants.Claims.ForNonce, nonce));
            }
        }

        // Add at_hash for implicit flow,
        // see https://openid.net/specs/openid-connect-core-1_0.html#TokenSubstitution
        if (additionalClaims.TryGetValue(AuthenticationConstants.Claims.ForAtHash, out var value2))
        {
            if (value2.ToString().HasValue())
            {
                var atHash = value2.ToString()!;
                claims.Add(new Claim(AuthenticationConstants.Claims.ForAtHash, atHash));
            }
        }

        // Add c_hash for hybrid flow,
        // see https://openid.net/specs/openid-connect-core-1_0.html#CodeValidation
        if (additionalClaims.TryGetValue(AuthenticationConstants.Claims.ForCHash, out var value3))
        {
            if (value3.ToString().HasValue())
            {
                var nonce = value3.ToString()!;
                claims.Add(new Claim(AuthenticationConstants.Claims.ForCHash, nonce));
            }
        }

        // Add auth_time for refresh token rotation,
        // see https://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation
        if (additionalClaims.TryGetValue(AuthenticationConstants.Claims.ForAuthTime, out var value4))
        {
            if (value4 is DateTime dateTime)
            {
                var authTime = new DateTimeOffset(dateTime).ToUnixTimeSeconds().ToString();
                claims.Add(new Claim(AuthenticationConstants.Claims.ForAuthTime, authTime));
            }
        }

        return claims;
    }

    /// <summary>
    ///     Returns the claims for the anonymous user
    /// </summary>
    public static IReadOnlyList<Claim> ToClaimsForAnonymousUser()
    {
        return
        [
            new Claim(AuthenticationConstants.Claims.ForId, CallerConstants.AnonymousUserId)
        ];
    }

    /// <summary>
    ///     Returns the claims for a service account
    /// </summary>
    public static IReadOnlyList<Claim> ToClaimsForServiceAccount()
    {
        return
        [
            new Claim(AuthenticationConstants.Claims.ForId, CallerConstants.MaintenanceAccountUserId),
            new Claim(AuthenticationConstants.Claims.ForRole,
                ToPlatformClaimValue(PlatformRoles.ServiceAccount)),
            new Claim(AuthenticationConstants.Claims.ForFeature, ToPlatformClaimValue(PlatformFeatures.Basic))
        ];
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