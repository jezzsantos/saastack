using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.SmsDelivery;

public sealed class DeliveryFailureConfirmed : DomainEvent
{
    public DeliveryFailureConfirmed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DeliveryFailureConfirmed()
    {
    }

    public required string Reason { get; set; }

    public required string ReceiptId { get; set; }

    public required DateTime When { get; set; }
}