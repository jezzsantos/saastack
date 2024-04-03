using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Organizations;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string CreatedById { get; set; }

    public required string Name { get; set; }

    public OrganizationOwnership Ownership { get; set; }
}