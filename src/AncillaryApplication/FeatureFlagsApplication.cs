using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.FeatureFlags;

namespace AncillaryApplication;

public class FeatureFlagsApplication : IFeatureFlagsApplication
{
    private readonly IFeatureFlags _featureFlags;

    private readonly IRecorder _recorder;

    public FeatureFlagsApplication(IRecorder recorder, IFeatureFlags featureFlags)
    {
        _recorder = recorder;

        _featureFlags = featureFlags;
    }

    public async Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext context,
        CancellationToken cancellationToken)
    {
        var flags = await _featureFlags.GetAllFlagsAsync(cancellationToken);

        _recorder.TraceInformation(context.ToCall(), "Feature flags were retrieved");

        return flags.Value.ToList();
    }

    public async Task<Result<FeatureFlag, Error>> GetFeatureFlagAsync(ICallerContext context, string name,
        string? tenantId, string? userId, CancellationToken cancellationToken)
    {
        var flag = await _featureFlags.GetFlagAsync(new Flag(name), tenantId, userId, cancellationToken);
        if (!flag.IsSuccessful)
        {
            return flag.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Feature flag {Name} was retrieved", name);

        return flag.Value;
    }
}