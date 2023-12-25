using Application.Interfaces;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can report usages events
/// </summary>
public interface IUsageReportingService
{
    /// <summary>
    ///     Tracks the usage event
    /// </summary>
    Task<Result<Error>> TrackAsync(ICallerContext context, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default);
}