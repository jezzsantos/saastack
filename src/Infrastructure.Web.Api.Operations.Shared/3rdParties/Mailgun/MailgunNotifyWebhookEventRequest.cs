using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;

/// <summary>
///     Notifies a Mailgun event, via a webhook
/// </summary>
[Route("/webhooks/mailgun", OperationMethod.Post)]
public class MailgunNotifyWebhookEventRequest : IWebRequest<EmptyResponse>
{
    [JsonPropertyName("event-data")] public MailgunEventData? EventData { get; set; }

    public MailgunSignature? Signature { get; set; }
}
