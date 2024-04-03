using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Cars;

public sealed class ManufacturerChanged : DomainEvent
{
    public ManufacturerChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ManufacturerChanged()
    {
    }

    public required string Make { get; set; }

    public required string Model { get; set; }

    public required string OrganizationId { get; set; }

    public required int Year { get; set; }
}