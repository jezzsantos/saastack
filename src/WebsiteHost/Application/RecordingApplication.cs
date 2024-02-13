using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Common.Recording;
using Task = System.Threading.Tasks.Task;

namespace WebsiteHost.Application;

public class RecordingApplication : IRecordingApplication
{
    private readonly IRecorder _recorder;

    public RecordingApplication(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<Error>> RecordCrashAsync(ICallerContext context, string message,
        CancellationToken cancellationToken)
    {
        var exceptionMessage = Resources.RecordingApplication_RecordCrash_ExceptionMessage.Format(message);
        _recorder.Crash(context.ToCall(), CrashLevel.Critical, new Exception(exceptionMessage));

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordMeasurementAsync(ICallerContext context, string eventName,
        Dictionary<string, object?>? additional, ClientDetails clientDetails,
        CancellationToken cancellationToken)
    {
        var more = AddClientContext(clientDetails, (additional.Exists()
            ? additional
                .Where(pair => pair.Value.Exists())
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : null)!);
        _recorder.Measure(context.ToCall(), eventName, more);

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordPageViewAsync(ICallerContext context, string path, ClientDetails clientDetails,
        CancellationToken cancellationToken)
    {
        const string eventName = UsageConstants.Events.Web.WebPageVisit;

        var additional = AddClientContext(clientDetails, new Dictionary<string, object>
        {
            { UsageConstants.Properties.Path, path }
        });

        _recorder.TrackUsage(context.ToCall(), eventName, additional);

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordUsageAsync(ICallerContext context, string eventName,
        Dictionary<string, object?>? additional, ClientDetails clientDetails,
        CancellationToken cancellationToken)
    {
        var more = AddClientContext(clientDetails, (additional.Exists()
            ? additional
                .Where(pair => pair.Value.Exists())
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : null)!);
        _recorder.TrackUsage(context.ToCall(), eventName, more);

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordTraceAsync(ICallerContext context, RecorderTraceLevel level,
        string messageTemplate, List<string>? arguments,
        CancellationToken cancellationToken)
    {
        var args = arguments.Exists()
            ? arguments.Select(arg => (object)arg).ToArray()
            : Array.Empty<object>();

        switch (level)
        {
            case RecorderTraceLevel.Debug:
                _recorder.TraceDebug(context.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            case RecorderTraceLevel.Information:
                _recorder.TraceInformation(context.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            case RecorderTraceLevel.Warning:
                _recorder.TraceWarning(context.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            case RecorderTraceLevel.Error:
                _recorder.TraceError(context.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            default:
                _recorder.TraceInformation(context.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);
        }
    }

    private static Dictionary<string, object> AddClientContext(ClientDetails clientDetails,
        IDictionary<string, object> additional)
    {
        var more = new Dictionary<string, object>(additional);
        more.TryAdd(UsageConstants.Properties.Timestamp, DateTime.UtcNow);
        more.TryAdd(UsageConstants.Properties.IpAddress, clientDetails.IpAddress.HasValue()
            ? clientDetails.IpAddress
            : "unknown");
        more.TryAdd(UsageConstants.Properties.UserAgent, clientDetails.UserAgent.HasValue()
            ? clientDetails.UserAgent
            : "unknown");
        more.TryAdd(UsageConstants.Properties.ReferredBy, clientDetails.Referer.HasValue()
            ? clientDetails.Referer
            : "unknown");
        more.TryAdd(UsageConstants.Properties.Component, UsageConstants.Components.BackEndForFrontEndWebHost);

        return more;
    }
}