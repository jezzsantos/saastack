using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;

/// <summary>
///     Notifies a Twilio event, via a webhook
/// </summary>
[Route("/webhooks/twilio", OperationMethod.Post)]
public class TwilioNotifyWebhookEventRequest : WebRequestEmpty<TwilioNotifyWebhookEventRequest>, IHasFormUrlEncoded
{
    public string? ApiVersion { get; set; }

    public string? ErrorCode { get; set; }

    public string? From { get; set; }

    public string? MessageSid { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TwilioMessageStatus? MessageStatus { get; set; }

    public long? RawDlrDoneDate { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TwilioMessageStatus? SmsStatus { get; set; }

    public string? To { get; set; }
}