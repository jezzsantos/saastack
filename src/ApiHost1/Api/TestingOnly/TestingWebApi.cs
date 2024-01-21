#if TESTINGONLY
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;

namespace ApiHost1.Api.TestingOnly;

public sealed class TestingWebApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;

    public TestingWebApi(ICallerContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ReSharper disable once InconsistentNaming
    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthNHMAC(
        GetCallerWithHMACTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _contextFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthNToken(
        GetCallerWithTokenOrAPIKeyTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _contextFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthZAnonymous(
        AuthorizeByNothingTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _contextFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthZByRole(
        AuthorizeByRoleTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _contextFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthZByFeature(
        AuthorizeByFeatureTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _contextFactory.Create().CallerId });
    }
    
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ContentNegotiationGet(
        ContentNegotiationsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = "amessage" });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ErrorsError(
        ErrorsErrorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(Error.EntityExists());
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ErrorsThrows(
        ErrorsThrowTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("amessage");
    }

    public async Task<ApiPostResult<string, FormatsTestingOnlyResponse>> FormatsRoundTrip(
        FormatsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return () => new Result<PostResult<FormatsTestingOnlyResponse>, Error>(new FormatsTestingOnlyResponse
        {
            Custom = new CustomDto
            {
                Time = request.Custom?.Time,
                Double = request.Custom?.Double,
                Integer = request.Custom?.Integer,
                String = request.Custom?.String,
                Enum = request.Custom?.Enum
            },
            Double = request.Double,
            Integer = request.Integer,
            String = request.String,
            Time = request.Time,
            Enum = request.Enum
        });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> RequestCorrelationGet(
        RequestCorrelationsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = "amessage" });
    }

    public async Task<ApiDeleteResult> StatusesDelete(StatusesDeleteTestingOnlyRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<EmptyResponse, Error>(new EmptyResponse());
    }

    public async Task<ApiGetResult<string, StatusesTestingOnlyResponse>> StatusesGet(
        StatusesGetTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiPostResult<string, StatusesTestingOnlyResponse>> StatusesPost(
        StatusesPostTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StatusesTestingOnlyResponse>(new StatusesTestingOnlyResponse { Message = "amessage" },
                "alocation");
    }

    public async Task<ApiPostResult<string, StatusesTestingOnlyResponse>> StatusesPost2(
        StatusesPostWithLocationTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StatusesTestingOnlyResponse>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiPutPatchResult<string, StatusesTestingOnlyResponse>> StatusesPutPatch(
        StatusesPutPatchTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiSearchResult<string, StatusesTestingOnlySearchResponse>> StatusesSearch(
        StatusesSearchTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlySearchResponse, Error>(new StatusesTestingOnlySearchResponse
                { Messages = new List<string> { "amessage" } });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ValidationsUnvalidated(
        ValidationsUnvalidatedTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Id}" });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ValidationsValidated(
        ValidationsValidatedTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Field1}" });
    }
}
#endif