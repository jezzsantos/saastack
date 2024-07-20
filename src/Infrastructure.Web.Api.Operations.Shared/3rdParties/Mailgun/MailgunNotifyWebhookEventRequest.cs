using System.Text.Json.Serialization;
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

public class MailgunEventData
{
    [JsonPropertyName("delivery-status")] public MailgunDeliveryStatus? DeliveryStatus { get; set; }

    public string? Event { get; set; }

    public string? Id { get; set; }

    public MailgunMessage? Message { get; set; }

    public string? Reason { get; set; }

    public string? Severity { get; set; }

    public double? Timestamp { get; set; }
}

public class MailgunDeliveryStatus
{
    public string? Description { get; set; }
}

public class MailgunSignature
{
    public string Signature { get; set; } = "";

    public string Timestamp { get; set; } = "";

    public string Token { get; set; } = "";
}

public class MailgunMessage
{
    public MailgunMessageHeaders? Headers { get; set; }
}

public class MailgunMessageHeaders
{
    public string? From { get; set; }

    [JsonPropertyName("message-id")] public string? MessageId { get; set; }

    public string? Subject { get; set; }

    public string? To { get; set; }
}