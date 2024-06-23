using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class PlatformFeatureUnassigned : DomainEvent
{
    public PlatformFeatureUnassigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PlatformFeatureUnassigned()
    {
    }

    public required string Feature { get; set; }
}