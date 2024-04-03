using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class DeliveryAttempted : IDomainEvent
{
    public required DateTime When { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string RootId { get; set; }
}