using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class DeliveryConfirmed : DomainEvent
{
    public DeliveryConfirmed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DeliveryConfirmed()
    {
    }

    public required DateTime When { get; set; }

    public required string ReceiptId { get; set; }
}