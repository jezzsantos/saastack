using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Bookings;

public sealed class CarChanged : DomainEvent
{
    public CarChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public CarChanged()
    {
    }

    public required string CarId { get; set; }

    public required string OrganizationId { get; set; }
}