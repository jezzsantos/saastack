using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Recording;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="IMetricReporter" /> that does nothing
/// </summary>
[ExcludeFromCodeCoverage]
public class NullMetricReporter : IMetricReporter
{
    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
    }
}