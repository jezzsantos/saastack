using System.Text.Json;
using Application.Common;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.UserPilot;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;
using Polly;

namespace Infrastructure.Shared.ApplicationServices.External;

public interface IUserPilotClient
{
    Task<Result<Error>> IdentifyUserAsync(ICallContext caller, string userId, Dictionary<string, string> metadata,
        Dictionary<string, string> company, CancellationToken cancellationToken);

    Task<Result<Error>> TrackEventAsync(ICallContext caller, string userId, string eventName,
        Dictionary<string, string> metadata, CancellationToken cancellationToken);
}

public sealed class UserPilotClient : IUserPilotClient
{
    private const string APIVersionHeaderName = "X-API-Version";
    private const string APIKeySettingName = "ApplicationServices:UserPilot:ApiKey";
    private const string BaseUrlSettingName = "ApplicationServices:UserPilot:BaseUrl";
    private readonly string _apiKey;
    private readonly IRecorder _recorder;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IServiceClient _serviceClient;

    public UserPilotClient(IRecorder recorder, IConfigurationSettings settings, IHttpClientFactory httpClientFactory)
        : this(recorder, settings.GetString(BaseUrlSettingName), settings.GetString(APIKeySettingName),
            ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter(), httpClientFactory)
    {
    }

    internal UserPilotClient(IRecorder recorder, IServiceClient serviceClient, IAsyncPolicy retryPolicy, string apiKey)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _retryPolicy = retryPolicy;
        _apiKey = apiKey;
    }

    private UserPilotClient(IRecorder recorder, string baseUrl, string apiKey, IAsyncPolicy retryPolicy,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new ApiServiceClient(httpClientFactory, JsonSerializerOptions.Default, baseUrl), retryPolicy, apiKey)
    {
    }

    public async Task<Result<Error>> IdentifyUserAsync(ICallContext call, string userId,
        Dictionary<string, string> metadata, Dictionary<string, string> company,
        CancellationToken cancellationToken)
    {
        var caller = Caller.CreateAsCallerFromCall(call);
        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () => await _serviceClient.PostAsync(caller,
                new UserPilotIdentifyUserRequest
                {
                    UserId = userId,
                    Metadata = metadata,
                    Company = company
                }, req => PrepareRequest(req, _apiKey), cancellationToken));
            if (response.IsFailure)
            {
                return response.Error.ToError();
            }

            return Result.Ok;
        }
        catch (HttpRequestException ex)
        {
            _recorder.TraceError(call, ex, "Error identifying UserPilot user {User}", userId);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    public async Task<Result<Error>> TrackEventAsync(ICallContext call, string userId, string eventName,
        Dictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        var caller = Caller.CreateAsCallerFromCall(call);
        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () => await _serviceClient.PostAsync(caller,
                new UserPilotTrackEventRequest
                {
                    UserId = userId,
                    EventName = eventName,
                    Metadata = metadata
                }, req => PrepareRequest(req, _apiKey), cancellationToken));
            if (response.IsFailure)
            {
                return response.Error.ToError();
            }

            return Result.Ok;
        }
        catch (HttpRequestException ex)
        {
            _recorder.TraceError(call, ex, "Error tracking UserPilot event {Event} for user {User}", eventName,
                userId);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private static void PrepareRequest(HttpRequestMessage message, string apiKey)
    {
        message.Headers.Add(HttpConstants.Headers.Authorization, $"token {apiKey}");
        message.Headers.Add(APIVersionHeaderName, "2020-09-22");
    }
}