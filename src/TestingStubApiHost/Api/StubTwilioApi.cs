using Application.Common;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;
using Infrastructure.Web.Interfaces.Clients;

namespace TestingStubApiHost.Api;

[BaseApiFrom("/twilio")]
public class StubTwilioApi : StubApiBase
{
    private readonly IServiceClient _serviceClient;

    public StubTwilioApi(IRecorder recorder, IConfigurationSettings settings, IServiceClient serviceClient) : base(
        recorder, settings)
    {
        _serviceClient = serviceClient;
    }

    public async Task<ApiPostResult<string, TwilioSendResponse>> SendMessage(TwilioSendRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubTwilio: SendMessage to {To}, from {From}, with body {Body}, with callback to {CallBack}",
            request.To!, request.From!, request.Body!, request.StatusCallback ?? string.Empty);

        // Fire the webhook event after returning
        var receiptId = $"receipt_{Guid.NewGuid():N}";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(() =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _serviceClient.PostAsync(Caller.CreateAsAnonymous(), new TwilioNotifyWebhookEventRequest
            {
                MessageStatus = TwilioMessageStatus.Delivered,
                ErrorCode = null
            }), cancellationToken);

        return () =>
            new PostResult<TwilioSendResponse>(new TwilioSendResponse
            {
                Sid = receiptId,
                Status = TwilioMessageStatus.Queued
            });
    }
}