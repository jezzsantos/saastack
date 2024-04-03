using Domain.Interfaces.Entities;
using Domain.Shared.Cars;

namespace Domain.Events.Shared.Cars;

public sealed class UnavailabilitySlotAdded : IDomainEvent
{
    public required UnavailabilityCausedBy CausedByReason { get; set; }

    public string? CausedByReference { get; set; }

    public required DateTime From { get; set; }

    public required string OrganizationId { get; set; }

    public required DateTime To { get; set; }

    public string? UnavailabilityId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}