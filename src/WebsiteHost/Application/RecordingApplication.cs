using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace WebsiteHost.Application;

public class RecordingApplication : IRecordingApplication
{
    private readonly IRecorder _recorder;

    public RecordingApplication(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<Error>> RecordCrashAsync(ICallerContext caller, string message,
        CancellationToken cancellationToken)
    {
        var exceptionMessage = Resources.RecordingApplication_RecordCrash_ExceptionMessage.Format(message);
        _recorder.Crash(caller.ToCall(), CrashLevel.Critical, new Exception(exceptionMessage));

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordMeasurementAsync(ICallerContext caller, string eventName,
        Dictionary<string, object?>? additional, ClientDetails clientDetails,
        CancellationToken cancellationToken)
    {
        var more = AddClientContext(caller, clientDetails, (additional.Exists()
            ? additional
                .Where(pair => pair.Value.Exists())
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : null)!);
        _recorder.Measure(caller.ToCall(), eventName, more);

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordPageViewAsync(ICallerContext caller, string path, ClientDetails clientDetails,
        CancellationToken cancellationToken)
    {
        var additional = AddClientContext(caller, clientDetails, new Dictionary<string, object>
        {
            { UsageConstants.Properties.Path, path }
        });
        
        const string eventName = UsageConstants.Events.Web.WebPageVisit;
        if (additional.Remove(UsageConstants.Properties.ForId, out var forId))
        {
            _recorder.TrackUsageFor(caller.ToCall(), forId.ToString()!, eventName, additional);
        }
        else
        {
            _recorder.TrackUsage(caller.ToCall(), eventName, additional);
        }

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordTraceAsync(ICallerContext caller, RecorderTraceLevel level,
        string messageTemplate, List<string>? arguments,
        CancellationToken cancellationToken)
    {
        var args = arguments.Exists()
            ? arguments.Select(object (arg) => arg).ToArray()
            : [];

        switch (level)
        {
            case RecorderTraceLevel.Debug:
                _recorder.TraceDebug(caller.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            case RecorderTraceLevel.Information:
                _recorder.TraceInformation(caller.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            case RecorderTraceLevel.Warning:
                _recorder.TraceWarning(caller.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            case RecorderTraceLevel.Error:
                _recorder.TraceError(caller.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);

            default:
                _recorder.TraceInformation(caller.ToCall(), messageTemplate, args);
                return Task.FromResult(Result.Ok);
        }
    }

    public Task<Result<Error>> RecordUsageAsync(ICallerContext caller, string eventName,
        Dictionary<string, object?>? additional, ClientDetails clientDetails,
        CancellationToken cancellationToken)
    {
        var more = AddClientContext(caller, clientDetails, (additional.Exists()
            ? additional
                .Where(pair => pair.Value.Exists())
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : null)!);
        if (more.Remove(UsageConstants.Properties.ForId, out var forId))
        {
            _recorder.TrackUsageFor(caller.ToCall(), forId.ToString()!, eventName, more);
        }
        else
        {
            _recorder.TrackUsage(caller.ToCall(), eventName, more);
        }

        return Task.FromResult(Result.Ok);
    }

    private static Dictionary<string, object> AddClientContext(ICallerContext caller, ClientDetails clientDetails,
        IDictionary<string, object> additional)
    {
        var more = new Dictionary<string, object>(additional);
        if (caller.CallerId.HasValue() && CallerConstants.IsAnonymousUser(caller.CallerId))
        {
            more.TryAdd(UsageConstants.Properties.ForId, caller.CallerId);
        }
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