using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class AvatarRemoved : DomainEvent
{
    public AvatarRemoved(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public AvatarRemoved()
    {
    }

    public required string AvatarId { get; set; }
}