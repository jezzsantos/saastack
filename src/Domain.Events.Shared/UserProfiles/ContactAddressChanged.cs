using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class ContactAddressChanged : DomainEvent
{
    public ContactAddressChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ContactAddressChanged()
    {
    }

    public string? City { get; set; }

    public required string CountryCode { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? State { get; set; }

    public required string UserId { get; set; }

    public string? Zip { get; set; }
}