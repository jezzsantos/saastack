using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Cars;

public sealed class OwnershipChanged : DomainEvent
{
    public OwnershipChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public OwnershipChanged()
    {
    }

    public required List<string> Managers { get; set; }

    public required string OrganizationId { get; set; }

    public required string Owner { get; set; }
}