using Common;
using Common.FeatureFlags;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IFeatureFlags" />
/// </summary>
public sealed class StubFeatureFlags : IFeatureFlags
{
    public string? LastGetFlag { get; private set; }

    public Task<Result<IReadOnlyList<FeatureFlag>, Error>> GetAllFlagsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(
            new Result<IReadOnlyList<FeatureFlag>, Error>((IReadOnlyList<FeatureFlag>)new List<FeatureFlag>()));
    }

    public Task<Result<FeatureFlag, Error>> GetFlagAsync(Flag flag, Optional<string> tenantId, Optional<string> userId,
        CancellationToken cancellationToken)
    {
        LastGetFlag = flag.Name;
        return Task.FromResult(new Result<FeatureFlag, Error>(new FeatureFlag
        {
            Name = flag.Name,
            IsEnabled = true
        }));
    }

    public bool IsEnabled(Flag flag)
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(Flag flag, string userId)
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(Flag flag, Optional<string> tenantId, string userId)
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        LastGetFlag = null;
    }
}