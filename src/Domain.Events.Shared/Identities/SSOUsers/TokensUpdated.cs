using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Identities.SSOUsers;

public sealed class TokensUpdated : IDomainEvent
{
    public required string CountryCode { get; set; }

    public required string EmailAddress { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Timezone { get; set; }

    public required string Tokens { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}