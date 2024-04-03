using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Cars;

public sealed class OwnershipChanged : IDomainEvent
{
    public required List<string> Managers { get; set; }

    public required string OrganizationId { get; set; }

    public required string Owner { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}