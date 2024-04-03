using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class DeliverySucceeded : DomainEvent
{
    public DeliverySucceeded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DeliverySucceeded()
    {
    }

    public required DateTime When { get; set; }
}