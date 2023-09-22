using Common.Recording;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="IMetricReporter" /> that does nothing
/// </summary>
public class NullMetricReporter : IMetricReporter
{
    public void Measure(string eventName, Dictionary<string, object>? context = null)
    {
    }
}