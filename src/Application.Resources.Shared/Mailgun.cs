using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Application.Resources.Shared;

public static class MailgunConstants
{
    public const string AuditSourceName = "mailgun_email";

    public static class Values
    {
        public const string PermanentSeverity = "permanent";
        public const string TemporarySeverity = "temporary";
    }
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

public enum MailgunEventType
{
    Unknown = 0,
    [EnumMember(Value = "delivered")] Delivered,
    [EnumMember(Value = "failed")] Failed
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