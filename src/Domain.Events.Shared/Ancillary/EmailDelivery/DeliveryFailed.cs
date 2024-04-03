using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class DeliveryFailed : DomainEvent
{
    public DeliveryFailed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DeliveryFailed()
    {
    }

    public required DateTime When { get; set; }
}