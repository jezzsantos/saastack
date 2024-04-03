using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class PlatformRoleUnassigned : DomainEvent
{
    public PlatformRoleUnassigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PlatformRoleUnassigned()
    {
    }

    public required string Role { get; set; }
}