using System.Runtime.Serialization;

namespace Application.Resources.Shared;

public static class TwilioConstants
{
    public const string AuditSourceName = "twilio_sms";
}

/// <summary>
///     See <see href="https://www.twilio.com/docs/messaging/api/message-resource#message-status-values" />
/// </summary>
public enum TwilioMessageStatus
{
    Unknown = 0,
    [EnumMember(Value = "queued")] Queued,
    [EnumMember(Value = "sending")] Sending,
    [EnumMember(Value = "sent")] Sent,
    [EnumMember(Value = "failed")] Failed,
    [EnumMember(Value = "delivered")] Delivered,
    [EnumMember(Value = "undelivered")] Undelivered,
    [EnumMember(Value = "receiving")] Receiving,
    [EnumMember(Value = "received")] Received,
    [EnumMember(Value = "accepted")] Accepted,
    [EnumMember(Value = "scheduled")] Scheduled,
    [EnumMember(Value = "read")] Read,
    [EnumMember(Value = "canceled")] Canceled
}

public class TwilioEventData
{
    public string? ApiVersion { get; set; }

    public string? ErrorCode { get; set; }

    public string? From { get; set; }

    public required string MessageSid { get; set; }

    public TwilioMessageStatus MessageStatus { get; set; }

    public long? RawDlrDoneDate { get; set; }

    public TwilioMessageStatus SmsStatus { get; set; }

    public string? To { get; set; }
}