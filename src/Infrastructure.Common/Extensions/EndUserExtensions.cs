using System.Security.Claims;
using Application.Resources.Shared;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Infrastructure.Interfaces;

namespace Infrastructure.Common.Extensions;

public static class EndUserExtensions
{
    /// <summary>
    ///     Returns the claims for the specified <see cref="user" />
    /// </summary>
    public static IReadOnlyList<Claim> ToClaims(this EndUser user)
    {
        var claims = new List<Claim>
        {
            new(AuthenticationConstants.ClaimForId, user.Id)
        };
        user.Roles.ForEach(role => claims.Add(new Claim(AuthenticationConstants.ClaimForRole, role)));
        user.FeatureLevels.ForEach(feature =>
            claims.Add(new Claim(AuthenticationConstants.ClaimForFeatureLevel, feature)));

        return claims;
    }

    /// <summary>
    ///     Returns the claims for a service account
    /// </summary>
    public static IReadOnlyList<Claim> ToClaimsForServiceAccount()
    {
        return new[]
        {
            new Claim(AuthenticationConstants.ClaimForId, CallerConstants.MaintenanceAccountUserId),
            new Claim(AuthenticationConstants.ClaimForRole, PlatformRoles.ServiceAccount),
            new Claim(AuthenticationConstants.ClaimForFeatureLevel, PlatformFeatureLevels.Basic.Name)
        };
    }
}