using Application.Interfaces;
using Common;

namespace AncillaryApplication;

public interface IRecordingApplication
{
    Task<Result<Error>> RecordMeasurementAsync(ICallerContext context, string eventName,
        Dictionary<string, object?>? additional,
        CancellationToken cancellationToken);

    Task<Result<Error>> RecordUsageAsync(ICallerContext context, string eventName,
        Dictionary<string, object?>? additional,
        CancellationToken cancellationToken);
}