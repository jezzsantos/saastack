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

    public async Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var flags = await _featureFlags.GetAllFlagsAsync(cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Feature flags were retrieved");

        return flags.Value.ToList();
    }

    public async Task<Result<FeatureFlag, Error>> GetFeatureFlagForCallerAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken)
    {
        var flag = await _featureFlags.GetFlagAsync(new Flag(name), caller, cancellationToken);
        if (flag.IsFailure)
        {
            return flag.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Feature flag {Name} was retrieved for user {User} in tenant {Tenant}", name, caller.CallerId,
            caller.TenantId ?? "none");

        return flag.Value;
    }

    public async Task<Result<FeatureFlag, Error>> GetFeatureFlagAsync(ICallerContext caller, string name,
        string? tenantId, string userId, CancellationToken cancellationToken)
    {
        var flag = await _featureFlags.GetFlagAsync(new Flag(name), tenantId, userId, cancellationToken);
        if (flag.IsFailure)
        {
            return flag.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Feature flag {Name} was retrieved for user {User}", name, userId);

        return flag.Value;
    }
}