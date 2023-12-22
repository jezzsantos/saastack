using Application.Common;
using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="IUsageReportingService" /> that does nothing
/// </summary>
public class NullUsageReportingService : IUsageReportingService
{
    private readonly IRecorder _recorder;

    public NullUsageReportingService(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<Error>> TrackAsync(ICallerContext context, string forId, string eventName,
        Dictionary<string, string>? additional = null,
        CancellationToken cancellationToken = default)
    {
        var properties = additional.Exists()
            ? additional.ToJson()!
            : "none";
        _recorder.TraceInformation(context.ToCall(),
            $"{nameof(NullUsageReportingService)} tracks usage event {{Event}} for {{For}} with properties: {{Properties}}",
            eventName, forId, properties);

        return Task.FromResult(Result.Ok);
    }
}