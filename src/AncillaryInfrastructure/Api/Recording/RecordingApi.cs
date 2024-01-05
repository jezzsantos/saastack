using AncillaryApplication;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Recording;

public sealed class RecordingApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IRecordingApplication _recordingApplication;

    public RecordingApi(ICallerContextFactory contextFactory, IRecordingApplication recordingApplication)
    {
        _contextFactory = contextFactory;
        _recordingApplication = recordingApplication;
    }

    public async Task<ApiEmptyResult> RecordMeasurement(RecordMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordMeasurementAsync(_contextFactory.Create(), request.EventName,
            request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordUsage(RecordUseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordUsageAsync(_contextFactory.Create(), request.EventName,
            request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
}