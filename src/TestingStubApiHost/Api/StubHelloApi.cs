using Common;
using Common.Configuration;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly.Stubs;

namespace TestingStubApiHost.Api;

public sealed class StubHelloApi : StubApiBase
{
    public StubHelloApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiGetResult<string, HelloResponse>> Get(HelloRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "StubHello: Hello");
        return () => new Result<HelloResponse, Error>(new HelloResponse { Message = "hello" });
    }
}