using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Cars;

public sealed class Created : IDomainEvent
{
    public required string OrganizationId { get; set; }

    public required string Status { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}