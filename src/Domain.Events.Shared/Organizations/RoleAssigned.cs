using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class RoleAssigned : DomainEvent
{
    public RoleAssigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RoleAssigned()
    {
    }

    public required string AssignedById { get; set; }

    public required string Role { get; set; }

    public required string UserId { get; set; }
}