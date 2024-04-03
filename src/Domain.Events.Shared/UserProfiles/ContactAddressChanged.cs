using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.UserProfiles;

public sealed class ContactAddressChanged : IDomainEvent
{
    public string? City { get; set; }

    public required string CountryCode { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? State { get; set; }

    public required string UserId { get; set; }

    public string? Zip { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}