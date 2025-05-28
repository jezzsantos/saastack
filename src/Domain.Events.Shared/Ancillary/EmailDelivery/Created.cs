using Common;
using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string HostRegion { get; set; } = DatacenterLocations.Unknown.Code;

    public required string MessageId { get; set; }

    public string? OrganizationId { get; set; }

    public required DateTime When { get; set; }
}