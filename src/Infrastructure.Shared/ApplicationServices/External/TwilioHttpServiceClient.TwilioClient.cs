using System.Text.Json;
using Application.Common;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;
using Polly;

namespace Infrastructure.Shared.ApplicationServices.External;

public interface ITwilioClient
{
    Task<Result<SmsDeliveryReceipt, Error>> SendAsync(ICallContext call, string toPhoneNumber, string body,
        CancellationToken cancellationToken);
}

/// <summary>
///     Defines a client to the Twilio API
///     See <see href="https://www.twilio.com/docs/messaging/api/message-resource#create-a-message-resource" />
/// </summary>
public class TwilioClient : ITwilioClient
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly IRecorder _recorder;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly string _senderPhoneNumber;
    private readonly IServiceClient _serviceClient;
    private readonly string _webhookCallbackUrl;

    public TwilioClient(IRecorder recorder, IConfigurationSettings settings, IHttpClientFactory httpClientFactory)
        : this(recorder,
            new ApiServiceClient(httpClientFactory, JsonSerializerOptions.Default,
                settings.GetString(Constants.BaseUrlSettingName)),
            ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter(),
            settings.GetString(Constants.AccountSidSettingName),
            settings.GetString(Constants.AuthTokenSettingName),
            settings.GetString(Constants.SenderPhoneNumberSettingName),
            settings.GetString(Constants.WebhookCallbackUrlSettingName)
        )
    {
    }

    private TwilioClient(IRecorder recorder, IServiceClient serviceClient, IAsyncPolicy retryPolicy, string accountSid,
        string authToken, string senderPhoneNumber,
        string webhookCallbackUrl)
    {
        _recorder = recorder;
        _retryPolicy = retryPolicy;
        _accountSid = accountSid;
        _senderPhoneNumber = senderPhoneNumber;
        _webhookCallbackUrl = webhookCallbackUrl;
        _authToken = authToken;
        _serviceClient = serviceClient;
    }

    public async Task<Result<SmsDeliveryReceipt, Error>> SendAsync(ICallContext call, string toPhoneNumber, string body,
        CancellationToken cancellationToken)
    {
        try
        {
            var caller = Caller.CreateAsCallerFromCall(call);
            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _serviceClient.PostAsync(caller, new TwilioSendRequest
                {
                    AccountSid = _accountSid,
                    StatusCallback = _webhookCallbackUrl,
                    To = toPhoneNumber,
                    From = _senderPhoneNumber,
                    Body = body
                }, req => req.SetBasicAuth(_accountSid, _authToken), cancellationToken));
            if (response.IsFailure)
            {
                return response.Error.ToError();
            }

            return new SmsDeliveryReceipt
            {
                ReceiptId = response.Value.Sid
            };
        }
        catch (HttpRequestException ex)
        {
            _recorder.TraceError(call, ex, "Error sending Twilio SMS to {To}", toPhoneNumber);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    public static class Constants
    {
        public const string AccountSidSettingName = "ApplicationServices:Twilio:AccountSid";
        public const string AuthTokenSettingName = "ApplicationServices:Twilio:AuthToken";
        public const string BaseUrlSettingName = "ApplicationServices:Twilio:BaseUrl";
        public const string SenderPhoneNumberSettingName = "ApplicationServices:Twilio:SenderPhoneNumber";
        public const string WebhookCallbackUrlSettingName = "ApplicationServices:Twilio:WebhookCallbackUrl";
    }
}