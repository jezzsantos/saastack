using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class RoleUnassigned : DomainEvent
{
    public RoleUnassigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RoleUnassigned()
    {
    }

    public required string Role { get; set; }

    public required string UnassignedById { get; set; }

    public required string UserId { get; set; }
}