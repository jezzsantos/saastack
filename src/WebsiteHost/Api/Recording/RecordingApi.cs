using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using WebsiteHost.Application;

namespace WebsiteHost.Api.Recording;

public sealed class RecordingApi : IWebApiService
{
    private readonly ICallerContext _context;
    private readonly IRecordingApplication _recordingApplication;

    public RecordingApi(ICallerContext context, IRecordingApplication recordingApplication)
    {
        _context = context;
        _recordingApplication = recordingApplication;
    }

    public async Task<ApiEmptyResult> RecordCrash(RecordCrashRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordCrashAsync(_context, request.Message,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordMeasurement(RecordMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordMeasurementAsync(_context, request.EventName, request.Additional,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordPageView(RecordPageViewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordPageViewAsync(_context, request.Path,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordTrace(RecordTraceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordTraceAsync(_context, request.Level.ToEnum<RecorderTraceLevel>(), request.MessageTemplate,
            request.Arguments,
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