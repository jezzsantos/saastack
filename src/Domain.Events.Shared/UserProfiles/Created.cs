using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.UserProfiles;

public sealed class Created : IDomainEvent
{
    public required string DisplayName { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Type { get; set; }

    public required string UserId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}