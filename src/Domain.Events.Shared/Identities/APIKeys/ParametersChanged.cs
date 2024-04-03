using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class ParametersChanged : IDomainEvent
{
    public required string Description { get; set; }

    public required DateTime ExpiresOn { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}