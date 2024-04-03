using Domain.Interfaces.Entities;
using Domain.Shared.Organizations;

namespace Domain.Events.Shared.Organizations;

public sealed class Created : IDomainEvent
{
    public required string CreatedById { get; set; }

    public required string Name { get; set; }

    public required OrganizationOwnership Ownership { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}