using Common;
using Common.Configuration;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Gravatar;

namespace TestingStubApiHost.Api;

[WebService("/gravatar")]
public sealed class StubGravatarApi : StubApiBase
{
    public StubGravatarApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiStreamResult> FindAvatar(GravatarGetImageRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "StubGravatar: FindAvatar");
        return () => new Result<StreamResult, Error>(Error.EntityNotFound());
    }
}