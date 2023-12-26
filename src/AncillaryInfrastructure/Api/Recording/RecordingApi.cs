using AncillaryApplication;
using Application.Interfaces;
using Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Recording;

public sealed class RecordingApi : IWebApiService
{
    private readonly ICallerContext _context;
    private readonly IRecordingApplication _recordingApplication;

    public RecordingApi(ICallerContext context, IRecordingApplication recordingApplication)
    {
        _context = context;
        _recordingApplication = recordingApplication;
    }

    public async Task<ApiEmptyResult> RecordMeasurement(RecordMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordMeasurementAsync(_context, request.EventName, request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordUsage(RecordUseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordUsageAsync(_context, request.EventName, request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
}