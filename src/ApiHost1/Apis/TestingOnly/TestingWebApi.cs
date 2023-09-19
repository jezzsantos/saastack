#if TESTINGONLY
using Infrastructure.WebApi.Interfaces;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

namespace ApiHost1.Apis.TestingOnly;

public class TestingWebApi : IWebApiService
{
    [WebApiRoute("/testingonly/{id}/unvalidated", WebApiOperation.Get, true)]
    public async Task<IResult> Get(GetWithoutValidatorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Results.Ok(new StringMessageTestingOnlyResponse { Message = $"amessage{request.Id}" });
    }

    [WebApiRoute("/testingonly/{id}/validated", WebApiOperation.Get, true)]
    public async Task<IResult> Get(GetWithValidatorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Results.Ok(new StringMessageTestingOnlyResponse { Message = $"amessage{request.Field1}" });
    }

    [WebApiRoute("/testingonly/throws", WebApiOperation.Get, true)]
    public async Task<IResult> Get(ThrowsExceptionTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("amessage");
    }
}
#endif