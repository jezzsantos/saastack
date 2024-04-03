using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.EndUsers;

public sealed class Created : IDomainEvent
{
    public required string Access { get; set; }

    public required string Classification { get; set; }

    public required string Status { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}