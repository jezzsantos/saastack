using AncillaryDomain;
using Application.Persistence.Common;
using Common;
using QueryAny;

namespace AncillaryApplication.Persistence.ReadModels;

[EntityName("SmsDelivery")]
public class SmsDelivery : ReadModelEntity
{
    public Optional<SendingAttempts> Attempts { get; set; }

    public Optional<string> Body { get; set; }

    public Optional<DateTime?> Created { get; set; }

    public Optional<DateTime?> Delivered { get; set; }

    public Optional<DateTime?> DeliveryFailed { get; set; }

    public Optional<string> DeliveryFailedReason { get; set; }

    public Optional<DateTime?> LastAttempted { get; set; }

    public Optional<string> MessageId { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<string> ReceiptId { get; set; }

    public Optional<DateTime?> SendFailed { get; set; }

    public Optional<DateTime?> Sent { get; set; }

    public Optional<string> Tags { get; set; }

    public Optional<string> ToPhoneNumber { get; set; }
}