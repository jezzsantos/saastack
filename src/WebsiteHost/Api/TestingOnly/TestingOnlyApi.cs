#if TESTINGONLY
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.TestingOnly;

namespace WebsiteHost.Api.TestingOnly;

[BaseApiFrom("/api")]
public sealed class TestingOnlyApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;

    public TestingOnlyApi(ICallerContextFactory callerFactory)
    {
        _callerFactory = callerFactory;
    }

    public async Task<ApiPostResult<string, BeffeTestingOnlyResponse>> AnonymousDirectPost(
        BeffeAnonymousDirectTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<PostResult<BeffeTestingOnlyResponse>, Error>(new BeffeTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiPostResult<string, BeffeTestingOnlyResponse>> AnonymousPost(
        BeffeAnonymousTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<PostResult<BeffeTestingOnlyResponse>, Error>(new BeffeTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiPostResult<string, BeffeTestingOnlyResponse>> HmacDirectPost(
        BeffeHMacDirectTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<PostResult<BeffeTestingOnlyResponse>, Error>(new BeffeTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }
}
#endif