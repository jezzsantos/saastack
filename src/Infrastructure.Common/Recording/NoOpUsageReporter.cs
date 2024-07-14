using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Recording;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="IUsageReporter" /> that does nothing
/// </summary>
[ExcludeFromCodeCoverage]
public class NoOpUsageReporter : IUsageReporter
{
    public Task<Result<Error>> TrackAsync(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok);
    }
}