using System.Text.Json;
using Application.Common;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;
using Infrastructure.Web.Common.Extensions;

namespace Infrastructure.External.ApplicationServices;

public interface IMixpanelClient
{
    Task<Result<Error>> ImportAsync(ICallContext call, string userId, string eventName,
        MixpanelEventProperties properties,
        CancellationToken cancellationToken);

    Task<Result<Error>> SetProfileAsync(ICallContext call, string userId, MixpanelProfileProperties properties,
        CancellationToken cancellationToken);
}

public class MixpanelClient : IMixpanelClient
{
    private const string BaseUrlSettingName = "ApplicationServices:Mixpanel:BaseUrl";
    private const string ProjectIdSettingName = "ApplicationServices:Mixpanel:ProjectId";
    private const string ProjectTokenSettingName = "ApplicationServices:Mixpanel:ProjectToken";
    private readonly string _projectId;
    private readonly string _projectToken;
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public MixpanelClient(IRecorder recorder, IConfigurationSettings settings, IHttpClientFactory clientFactory,
        JsonSerializerOptions jsonSerializerOptions) : this(recorder,
        new ApiServiceClient(clientFactory, jsonSerializerOptions, settings.Platform.GetString(BaseUrlSettingName)),
        settings.Platform.GetString(ProjectIdSettingName), settings.Platform.GetString(ProjectTokenSettingName))
    {
    }

    internal MixpanelClient(IRecorder recorder, IServiceClient serviceClient, string projectId,
        string projectToken)
    {
        _projectId = projectId;
        _projectToken = projectToken;
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<Error>> ImportAsync(ICallContext call, string userId, string eventName,
        MixpanelEventProperties properties, CancellationToken cancellationToken)
    {
        var caller = Caller.CreateAsCallerFromCall(call);
        try
        {
            var request = new MixpanelImportEventsRequest
            {
                new MixpanelImportEvent
                {
                    Event = eventName,
                    Properties = properties
                }
            };
            request.Strict = 1;
            request.ProjectId = _projectId;
            var response = await _serviceClient.PostAsync(caller,
                request, req => PrepareRequest(req, _projectToken), cancellationToken);
            if (response.IsFailure)
            {
                return response.Error.ToError();
            }

            return Result.Ok;
        }
        catch (HttpRequestException ex)
        {
            _recorder.TraceError(call, ex, "Error tracking Mixpanel event {Event} for user {User}",
                eventName, userId);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    public async Task<Result<Error>> SetProfileAsync(ICallContext call, string userId,
        MixpanelProfileProperties properties, CancellationToken cancellationToken)
    {
        var caller = Caller.CreateAsCallerFromCall(call);
        try
        {
            var request = new MixpanelSetProfileRequest
            {
                new MixpanelProfile
                {
                    Token = _projectToken,
                    DistinctId = userId,
                    Set = properties
                }
            };
            request.Verbose = 1;
            var response = await _serviceClient.PostAsync(caller,
                request, req => PrepareRequest(req, _projectToken), cancellationToken);
            if (response.IsFailure)
            {
                return response.Error.ToError();
            }

            return Result.Ok;
        }
        catch (HttpRequestException ex)
        {
            _recorder.TraceError(call, ex, "Error setting Mixpanel profile for {User}",
                userId);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private static void PrepareRequest(HttpRequestMessage message, string projectToken)
    {
        message.SetBasicAuth(projectToken, string.Empty);
    }
}