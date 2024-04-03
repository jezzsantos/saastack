using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.SSOUsers;

public sealed class Created : IDomainEvent
{
    public required string ProviderName { get; set; }

    public required string UserId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}