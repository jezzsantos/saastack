using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class RegistrationChanged : IDomainEvent
{
    public required string EmailAddress { get; set; }

    public required string Name { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}