using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.SSOUsers;

public sealed class DetailsAdded : DomainEvent
{
    public DetailsAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DetailsAdded()
    {
    }

    public required string CountryCode { get; set; }

    public required string EmailAddress { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Timezone { get; set; }
}