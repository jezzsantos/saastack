using AncillaryDomain;
using Application.Persistence.Common;
using Common;
using QueryAny;

namespace AncillaryApplication.Persistence.ReadModels;

[EntityName("EmailDelivery")]
public class EmailDelivery : ReadModelEntity
{
    public Optional<DeliveryAttempts> Attempts { get; set; }

    public Optional<string> Body { get; set; }

    public Optional<DateTime?> Delivered { get; set; }

    public Optional<DateTime?> Failed { get; set; }

    public Optional<DateTime?> LastAttempted { get; set; }

    public Optional<string> MessageId { get; set; }

    public Optional<string> Subject { get; set; }

    public Optional<string> ToDisplayName { get; set; }

    public Optional<string> ToEmailAddress { get; set; }
}