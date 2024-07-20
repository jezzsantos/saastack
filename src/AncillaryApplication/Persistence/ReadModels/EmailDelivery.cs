using AncillaryDomain;
using Application.Persistence.Common;
using Common;
using QueryAny;

namespace AncillaryApplication.Persistence.ReadModels;

[EntityName("EmailDelivery")]
public class EmailDelivery : ReadModelEntity
{
    public Optional<SendingAttempts> Attempts { get; set; }

    public Optional<string> Body { get; set; }

    public Optional<DateTime?> Delivered { get; set; }

    public Optional<DateTime?> DeliveryFailed { get; set; }

    public Optional<string> DeliveryFailedReason { get; set; }

    public Optional<DateTime?> LastAttempted { get; set; }

    public Optional<string> MessageId { get; set; }

    public Optional<string> ReceiptId { get; set; }

    public Optional<DateTime?> SendFailed { get; set; }

    public Optional<DateTime?> Sent { get; set; }

    public Optional<string> Subject { get; set; }

    public Optional<string> ToDisplayName { get; set; }

    public Optional<string> ToEmailAddress { get; set; }
}