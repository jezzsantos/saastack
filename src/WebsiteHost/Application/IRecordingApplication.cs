using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace WebsiteHost.Application;

public interface IRecordingApplication
{
    Task<Result<Error>> RecordCrashAsync(ICallerContext context, string message, CancellationToken cancellationToken);

    Task<Result<Error>> RecordMeasurementAsync(ICallerContext context, string eventName,
        Dictionary<string, object?>? additional, CancellationToken cancellationToken);

    Task<Result<Error>> RecordPageViewAsync(ICallerContext context, string path, CancellationToken cancellationToken);

    Task<Result<Error>> RecordTraceAsync(ICallerContext context, RecorderTraceLevel level, string messageTemplate,
        List<string>? arguments, CancellationToken cancellationToken);

    Task<Result<Error>> RecordUsageAsync(ICallerContext context, string eventName,
        Dictionary<string, object?>? additional, CancellationToken cancellationToken);
}