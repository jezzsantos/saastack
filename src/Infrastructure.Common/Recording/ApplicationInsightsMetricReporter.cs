#if HOSTEDONAZURE
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Services;
using Microsoft.ApplicationInsights;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides a <see cref="IMetricReporter" /> that sends its reports to Application Insights
/// </summary>
public class ApplicationInsightsMetricReporter : IMetricReporter
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsMetricReporter(IDependencyContainer container)
    {
        _telemetryClient = container.Resolve<TelemetryClient>();
    }

    public void Measure(string eventName, Dictionary<string, object>? additional = null)
    {
        if (_telemetryClient.Exists())
        {
            _telemetryClient.TrackEvent(eventName,
                (additional ?? new Dictionary<string, object>())
                .ToDictionary(pair => pair.Key, pair => pair.Value.Exists()
                    ? pair.Value.ToString()
                    : string.Empty));
        }
    }
}
#endif