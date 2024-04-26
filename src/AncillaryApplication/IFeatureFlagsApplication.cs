using Application.Interfaces;
using Common;
using Common.FeatureFlags;

namespace AncillaryApplication;

public interface IFeatureFlagsApplication
{
    Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    Task<Result<FeatureFlag, Error>> GetFeatureFlagAsync(ICallerContext caller, string name, string? tenantId,
        string userId, CancellationToken cancellationToken);

    Task<Result<FeatureFlag, Error>> GetFeatureFlagForCallerAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken);
}