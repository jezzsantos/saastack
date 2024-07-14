namespace Common.Recording;

/// <summary>
///     Records user's usage of the system
/// </summary>
public interface IUsageReporter
{
    /// <summary>
    ///     Tracks the usage
    /// </summary>
    Task<Result<Error>> TrackAsync(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null, CancellationToken cancellationToken = default);
}