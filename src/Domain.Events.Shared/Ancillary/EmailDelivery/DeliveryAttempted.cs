using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class DeliveryAttempted : DomainEvent
{
    public DeliveryAttempted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DeliveryAttempted()
    {
    }

    public required DateTime When { get; set; }
}