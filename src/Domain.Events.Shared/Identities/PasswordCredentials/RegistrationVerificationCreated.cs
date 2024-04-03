using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class RegistrationVerificationCreated : IDomainEvent
{
    public required string Token { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}