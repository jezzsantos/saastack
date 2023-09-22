namespace Common.Recording;

/// <summary>
///     Records user's usage of the system
/// </summary>
public interface IUsageReporter
{
    void Track(ICallContext? context, string forId, string eventName, Dictionary<string, object>? additional = null);
}