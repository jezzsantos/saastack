using Common;
using Common.FeatureFlags;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides a <see cref="IFeatureFlags" /> that has no feature flags, and all are enabled
/// </summary>
public class EmptyFeatureFlags : IFeatureFlags
{
    public Task<Result<IReadOnlyList<FeatureFlag>, Error>> GetAllFlagsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<IReadOnlyList<FeatureFlag>, Error>>(new List<FeatureFlag>());
    }

    public async Task<Result<FeatureFlag, Error>> GetFlagAsync(Flag flag, Optional<string> tenantId,
        Optional<string> userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new Result<FeatureFlag, Error>(new FeatureFlag
        {
            Name = flag.Name,
            IsEnabled = true
        });
    }

    public bool IsEnabled(Flag flag)
    {
        return true;
    }

    public bool IsEnabled(Flag flag, string userId)
    {
        return true;
    }

    public bool IsEnabled(Flag flag, Optional<string> tenantId, string userId)
    {
        return true;
    }
}