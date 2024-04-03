using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Bookings;

public sealed class CarChanged : IDomainEvent
{
    public required string CarId { get; set; }

    public required string OrganizationId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}