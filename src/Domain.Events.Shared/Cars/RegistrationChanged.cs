using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Cars;

public sealed class RegistrationChanged : IDomainEvent
{
    public required string Jurisdiction { get; set; }

    public required string Number { get; set; }

    public required string OrganizationId { get; set; }

    public required string Status { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}