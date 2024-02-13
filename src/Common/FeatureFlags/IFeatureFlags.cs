namespace Common.FeatureFlags;

/// <summary>
///     Defines a service that provides feature flags
/// </summary>
public interface IFeatureFlags
{
    /// <summary>
    ///     Returns all available feature flags
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task<Result<IReadOnlyList<FeatureFlag>, Error>> GetAllFlagsAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the feature flag and its state
    /// </summary>
    Task<Result<FeatureFlag, Error>> GetFlagAsync(Flag flag, Optional<string> tenantId, Optional<string> userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the <see cref="flag" /> is enabled
    /// </summary>
    bool IsEnabled(Flag flag);

    /// <summary>
    ///     Whether the <see cref="flag" /> is enabled for the specified <see cref="userId" />
    /// </summary>
    bool IsEnabled(Flag flag, string userId);

    /// <summary>
    ///     Whether the <see cref="flag" /> is enabled for the specified <see cref="userId" /> of the
    ///     specified <see cref="tenantId" /> in that tenant.
    /// </summary>
    bool IsEnabled(Flag flag, Optional<string> tenantId, string userId);
}