using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.UserPilot;

namespace TestingStubApiHost.Api;

[WebService("/userpilot")]
public class StubUserPilotApi : StubApiBase
{
    public StubUserPilotApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiEmptyResult> IdentifyUser(UserPilotIdentifyUserRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "StubUserPilot: IdentifyUser for {User} with {Metadata}, for company {Company}",
            request.UserId ?? "none", request.Metadata.ToJson()!, request.Company.ToJson()!);
        return () => new EmptyResponse();
    }

    public async Task<ApiEmptyResult> TrackEvent(UserPilotTrackEventRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "StubUserPilot: TrackEvent for {Event} for {User} with {Metadata}",
            request.EventName ?? "none", request.UserId ?? "none", request.Metadata.ToJson()!);
        return () => new EmptyResponse();
    }
}