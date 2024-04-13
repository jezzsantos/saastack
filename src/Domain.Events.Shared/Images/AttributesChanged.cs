using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Images;

public sealed class AttributesChanged : DomainEvent
{
    public AttributesChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public AttributesChanged()
    {
    }

    public required long Size { get; set; }
}