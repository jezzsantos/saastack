using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.UserProfiles;

public sealed class PhoneNumberChanged : IDomainEvent
{
    public required string Number { get; set; }

    public required string UserId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}