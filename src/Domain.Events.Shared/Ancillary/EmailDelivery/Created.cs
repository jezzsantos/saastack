using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class Created : IDomainEvent
{
    public required string MessageId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}