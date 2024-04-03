using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.UserProfiles;

public sealed class NameChanged : IDomainEvent
{
    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string UserId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}