using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Cars;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Cars;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string OrganizationId { get; set; }

    public required CarStatus Status { get; set; }
}