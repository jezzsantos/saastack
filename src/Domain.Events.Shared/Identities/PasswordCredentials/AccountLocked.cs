using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class AccountLocked : IDomainEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}