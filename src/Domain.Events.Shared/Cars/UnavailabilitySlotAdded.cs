using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Cars;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Cars;

public sealed class UnavailabilitySlotAdded : DomainEvent
{
    public UnavailabilitySlotAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public UnavailabilitySlotAdded()
    {
    }

    public required UnavailabilityCausedBy CausedByReason { get; set; }

    public string? CausedByReference { get; set; }

    public required DateTime From { get; set; }

    public required string OrganizationId { get; set; }

    public required DateTime To { get; set; }

    public string? UnavailabilityId { get; set; }
}