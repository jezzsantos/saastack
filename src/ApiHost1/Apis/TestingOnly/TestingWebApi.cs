#if TESTINGONLY
using Common;
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

namespace ApiHost1.Apis.TestingOnly;

public class TestingWebApi : IWebApiService
{
    [WebApiRoute("/testingonly/{id}/unvalidated", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> Get(
        GetWithoutValidatorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Id}" });
    }

    [WebApiRoute("/testingonly/{id}/validated", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> Get(
        GetWithValidatorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Field1}" });
    }

    [WebApiRoute("/testingonly/error", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> Get(
        ReturnsErrorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(Error.EntityExists());
    }

    [WebApiRoute("/testingonly/throws", WebApiOperation.Get, true)]
    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> Get(
        ThrowsExceptionTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("amessage");
    }

    [WebApiRoute("/testingonly/roundtrip", WebApiOperation.Post, true)]
    public async Task<ApiResult<string, DataTypesTestingOnlyResponse>> Post(
        PostRoundTripDatesTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return () => new Result<DataTypesTestingOnlyResponse, Error>(new DataTypesTestingOnlyResponse
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
}
#endif