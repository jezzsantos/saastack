using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class EmailDetailsChanged : IDomainEvent
{
    public required string Body { get; set; }

    public required string Subject { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}