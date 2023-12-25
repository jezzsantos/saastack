#if HOSTEDONAZURE
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Services;
using Microsoft.ApplicationInsights;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides a <see cref="ICrashReporter" /> that sends its reports to Application Insights
/// </summary>
public class ApplicationInsightsCrashReporter : ICrashReporter
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsCrashReporter(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public ApplicationInsightsCrashReporter(IDependencyContainer container)
    {
        _telemetryClient = container.Resolve<TelemetryClient>();
    }

    public void Crash(ICallContext? call, CrashLevel level, Exception exception, string messageTemplate,
        object[] templateArgs)
    {
        if (_telemetryClient.Exists())
        {
            var properties = templateArgs
                .Select(arg => arg.ToString())
                .Join(", ");

            var context = new Dictionary<string, string>
            {
                { "Level", level.ToString() },
                { "Message_Template", messageTemplate },
                { "Message_Properties", properties }
            };
            if (call.Exists())
            {
                context.Add(nameof(ICallContext.CallId), call.CallId);
                context.Add(nameof(ICallContext.CallerId), call.CallerId);
            }

            _telemetryClient.TrackException(exception, context);
        }
    }
}
#endif