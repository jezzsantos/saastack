using Application.Interfaces;
using Common;
using Common.FeatureFlags;

namespace AncillaryApplication;

public interface IFeatureFlagsApplication
{
    Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext context,
        CancellationToken cancellationToken);

    Task<Result<FeatureFlag, Error>> GetFeatureFlagAsync(ICallerContext context, string name, string? tenantId,
        string userId, CancellationToken cancellationToken);

    Task<Result<FeatureFlag, Error>> GetFeatureFlagForCallerAsync(ICallerContext context, string name,
        CancellationToken cancellationToken);
}