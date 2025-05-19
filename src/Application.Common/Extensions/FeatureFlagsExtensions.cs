using Application.Interfaces;
using Common;
using Common.FeatureFlags;

namespace Application.Common.Extensions;

public static class FeatureFlagsExtensions
{
    /// <summary>
    ///     Returns the specified feature for the <see cref="caller" />
    /// </summary>
    public static async Task<Result<FeatureFlag, Error>> GetFlagAsync(this IFeatureFlags featureFlags, Flag flag,
        ICallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated)
        {
            return await featureFlags.GetFlagAsync(flag, Optional<string>.None, Optional<string>.None,
                cancellationToken);
        }

        if (caller.TenantId.HasValue)
        {
            return await featureFlags.GetFlagAsync(flag, caller.TenantId.Value, caller.CallerId, cancellationToken);
        }

        return await featureFlags.GetFlagAsync(flag, Optional<string>.None, caller.CallerId, cancellationToken);
    }

    /// <summary>
    ///     Whether the specified feature is enabled for the <see cref="caller" />
    /// </summary>
    public static bool IsEnabled(this IFeatureFlags featureFlags, Flag flag, ICallerContext caller)
    {
        if (!caller.IsAuthenticated)
        {
            return featureFlags.IsEnabled(flag);
        }

        if (caller.TenantId.HasValue)
        {
            return featureFlags.IsEnabled(flag, caller.TenantId.Value, caller.CallerId);
        }

        return featureFlags.IsEnabled(flag, caller.CallerId);
    }
}