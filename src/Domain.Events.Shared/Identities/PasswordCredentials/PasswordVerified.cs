using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class PasswordVerified : IDomainEvent
{
    public required bool AuditAttempt { get; set; }

    public required bool IsVerified { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}