using Application.Common;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;
using Infrastructure.Web.Interfaces.Clients;

namespace TestingStubApiHost.Api;

[BaseApiFrom("/mailgun")]
public class StubMailgunApi : StubApiBase
{
    private readonly IServiceClient _serviceClient;

    public StubMailgunApi(IRecorder recorder, IConfigurationSettings settings, IServiceClient serviceClient) : base(
        recorder, settings)
    {
        _serviceClient = serviceClient;
    }

    public async Task<ApiPostResult<string, MailgunSendResponse>> SendMessage(MailgunSendRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubMailgun: SendMessage to {To}{Recipient}, from {From}, with subject {Subject}, and body {Body}",
            request.To!, request.RecipientVariables!, request.From!, request.Subject!, request.Html!);

        // Fire the webhook event after returning
        var receiptId = $"receipt_{Guid.NewGuid():N}";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(() =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _serviceClient.PostAsync(Caller.CreateAsAnonymous(), new MailgunNotifyWebhookEventRequest
            {
                Signature = new MailgunSignature
                {
                    Timestamp = "1",
                    Token = "atoken",
                    Signature = "bf106940253fa7477ba4b55a027126b70037ce9b00e67aa3bf4f5bab2775d3e1"
                },
                EventData = new MailgunEventData
                {
                    Event = MailgunEventType.Delivered.ToString(),
                    Timestamp = DateTime.UtcNow.ToUnixSeconds(),
                    Message = new MailgunMessage
                    {
                        Headers = new MailgunMessageHeaders
                        {
                            MessageId = receiptId
                        }
                    }
                }
            }), cancellationToken);

        return () =>
            new PostResult<MailgunSendResponse>(new MailgunSendResponse
            {
                Id = receiptId,
                Message = "Queued. Thank you."
            });
    }
}