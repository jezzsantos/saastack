using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class PlatformFeatureAssigned : DomainEvent
{
    public PlatformFeatureAssigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PlatformFeatureAssigned()
    {
    }

    public required string Feature { get; set; }
}