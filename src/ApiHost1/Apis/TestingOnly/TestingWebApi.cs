#if TESTINGONLY
using Common;
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

namespace ApiHost1.Apis.TestingOnly;

public class TestingWebApi : IWebApiService
{
    [WebApiRoute("testingonly/negotiations/get", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ContentNegotiationGet(
        ContentNegotiationsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = "amessage" });
    }

    [WebApiRoute("/testingonly/errors/error", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ErrorsError(
        ErrorsErrorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(Error.EntityExists());
    }

    [WebApiRoute("/testingonly/errors/throws", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ErrorsThrows(
        ErrorsThrowTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("amessage");
    }

    [WebApiRoute("/testingonly/formats/roundtrip", WebApiOperation.Post, true)]
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

    [WebApiRoute("testingonly/correlations/get", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> RequestCorrelationGet(
        RequestCorrelationsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = "amessage" });
    }

    [WebApiRoute("/testingonly/statuses/delete", WebApiOperation.Delete, true)]
    public async Task<ApiEmptyResult> StatusesDelete(StatusesDeleteTestingOnlyRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<EmptyResponse, Error>(new EmptyResponse());
    }

    [WebApiRoute("/testingonly/statuses/get", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StatusesTestingOnlyResponse>> StatusesGet(StatusesGetTestingOnlyRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    [WebApiRoute("/testingonly/statuses/post", WebApiOperation.Post, true)]
    public async Task<ApiPostResult<string, StatusesTestingOnlyResponse>> StatusesPost(
        StatusesPostTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StatusesTestingOnlyResponse>(new StatusesTestingOnlyResponse { Message = "amessage" },
                "alocation");
    }

    [WebApiRoute("/testingonly/statuses/post2", WebApiOperation.Post, true)]
    public async Task<ApiResult<string, StatusesTestingOnlyResponse>> StatusesPost2(
        StatusesPostWithLocationTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    [WebApiRoute("/testingonly/statuses/putpatch", WebApiOperation.PutPatch, true)]
    public async Task<ApiResult<string, StatusesTestingOnlyResponse>> StatusesPutPatch(
        StatusesPutPatchTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    [WebApiRoute("/testingonly/statuses/search", WebApiOperation.Search, true)]
    public async Task<ApiResult<string, StatusesTestingOnlyResponse>> StatusesSearch(
        StatusesSearchTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    [WebApiRoute("/testingonly/validations/unvalidated", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ValidationsUnvalidated(
        ValidationsUnvalidatedTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Id}" });
    }

    [WebApiRoute("/testingonly/validations/validated/{id}", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ValidationsValidated(
        ValidationsValidatedTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Field1}" });
    }
}
#endif