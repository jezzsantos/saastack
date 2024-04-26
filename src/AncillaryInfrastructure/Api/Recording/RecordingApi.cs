using AncillaryApplication;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Recording;

public sealed class RecordingApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IRecordingApplication _recordingApplication;

    public RecordingApi(ICallerContextFactory callerFactory, IRecordingApplication recordingApplication)
    {
        _callerFactory = callerFactory;
        _recordingApplication = recordingApplication;
    }

    public async Task<ApiEmptyResult> RecordMeasurement(RecordMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordMeasurementAsync(_callerFactory.Create(), request.EventName,
            request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordUsage(RecordUseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordUsageAsync(_callerFactory.Create(), request.EventName,
            request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
}