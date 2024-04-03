using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Cars;

public sealed class UnavailabilitySlotRemoved : IDomainEvent
{
    public required string OrganizationId { get; set; }

    public required string UnavailabilityId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}