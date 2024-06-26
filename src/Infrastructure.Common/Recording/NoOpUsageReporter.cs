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
    public void Track(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
    }
}