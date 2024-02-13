using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using WebsiteHost.Application;

namespace WebsiteHost.Api.Recording;

public sealed class RecordingApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRecordingApplication _recordingApplication;

    public RecordingApi(ICallerContextFactory contextFactory, IRecordingApplication recordingApplication,
        IHttpContextAccessor httpContextAccessor)
    {
        _contextFactory = contextFactory;
        _recordingApplication = recordingApplication;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiEmptyResult> RecordCrash(RecordCrashRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordCrashAsync(_contextFactory.Create(), request.Message,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordMeasurement(RecordMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordMeasurementAsync(_contextFactory.Create(), request.EventName,
            request.Additional, _httpContextAccessor.ToClientDetails(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordPageView(RecordPageViewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordPageViewAsync(_contextFactory.Create(), request.Path,
            _httpContextAccessor.ToClientDetails(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordTrace(RecordTraceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordTraceAsync(_contextFactory.Create(),
            request.Level.ToEnum<RecorderTraceLevel>(),
            request.MessageTemplate,
            request.Arguments,
            cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RecordUsage(RecordUseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordingApplication.RecordUsageAsync(_contextFactory.Create(), request.EventName,
            request.Additional, _httpContextAccessor.ToClientDetails(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
}

internal static class HttpContextConversionExtensions
{
    public static ClientDetails ToClientDetails(this IHttpContextAccessor contextAccessor)
    {
        if (contextAccessor.HttpContext.NotExists())
        {
            return new ClientDetails();
        }

        var context = contextAccessor.HttpContext;

        return new ClientDetails
        {
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Referer = context.Request.Headers.Referer.ToString()
        };
    }
}