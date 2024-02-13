using Application.Interfaces;
using Common;
using Common.FeatureFlags;

namespace WebsiteHost.Application;

public interface IFeatureFlagsApplication
{
    Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext context,
        CancellationToken cancellationToken);

    Task<Result<FeatureFlag, Error>> GetFeatureFlagForCallerAsync(ICallerContext context, string name,
        CancellationToken cancellationToken);
}