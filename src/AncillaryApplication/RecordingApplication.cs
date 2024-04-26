using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Task = System.Threading.Tasks.Task;

namespace AncillaryApplication;

public class RecordingApplication : IRecordingApplication
{
    private readonly IRecorder _recorder;

    public RecordingApplication(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<Error>> RecordMeasurementAsync(ICallerContext caller, string eventName,
        Dictionary<string, object?>? additional,
        CancellationToken cancellationToken)
    {
        _recorder.Measure(caller.ToCall(), eventName, (additional.Exists()
            ? additional
                .Where(pair => pair.Value.Exists())
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : null)!);

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> RecordUsageAsync(ICallerContext caller, string eventName,
        Dictionary<string, object?>? additional,
        CancellationToken cancellationToken)
    {
        _recorder.TrackUsage(caller.ToCall(), eventName, (additional.Exists()
            ? additional
                .Where(pair => pair.Value.Exists())
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : null)!);

        return Task.FromResult(Result.Ok);
    }
}