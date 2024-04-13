using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Images;

public sealed class DetailsChanged : DomainEvent
{
    public DetailsChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DetailsChanged()
    {
    }

    public string? Description { get; set; }

    public string? Filename { get; set; }
}