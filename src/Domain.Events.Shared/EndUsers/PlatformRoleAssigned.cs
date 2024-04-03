using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class PlatformRoleAssigned : DomainEvent
{
    public PlatformRoleAssigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PlatformRoleAssigned()
    {
    }

    public required string Role { get; set; }
}