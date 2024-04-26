using Application.Interfaces;
using Common;

namespace AncillaryApplication;

public interface IRecordingApplication
{
    Task<Result<Error>> RecordMeasurementAsync(ICallerContext caller, string eventName,
        Dictionary<string, object?>? additional,
        CancellationToken cancellationToken);

    Task<Result<Error>> RecordUsageAsync(ICallerContext caller, string eventName,
        Dictionary<string, object?>? additional,
        CancellationToken cancellationToken);
}