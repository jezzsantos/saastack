#if TESTINGONLY
using Infrastructure.WebApi.Interfaces;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

namespace ApiHost1.Services.TestingOnly;

public class TestingOnlyApi : IWebApiService
{
    [WebApiRoute("/testingonly/{id}", WebApiOperation.Get, true)]
    public async Task<IResult> Get(GetTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Results.Ok(new GetTestingOnlyResponse { Message = $"amessage{request.Id}" });
    }
}
#endif