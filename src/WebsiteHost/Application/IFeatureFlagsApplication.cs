using Application.Interfaces;
using Common;
using Common.FeatureFlags;

namespace WebsiteHost.Application;

public interface IFeatureFlagsApplication
{
    Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    Task<Result<FeatureFlag, Error>> GetFeatureFlagForCallerAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken);
}