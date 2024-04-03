using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Cars;

public sealed class UnavailabilitySlotRemoved : DomainEvent
{
    public UnavailabilitySlotRemoved(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public UnavailabilitySlotRemoved()
    {
    }

    public required string OrganizationId { get; set; }

    public required string UnavailabilityId { get; set; }
}