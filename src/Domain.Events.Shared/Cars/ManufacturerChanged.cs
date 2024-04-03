using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Cars;

public sealed class ManufacturerChanged : IDomainEvent
{
    public required string Make { get; set; }

    public required string Model { get; set; }

    public required string OrganizationId { get; set; }

    public required int Year { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}