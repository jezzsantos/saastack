using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class NameChanged : DomainEvent
{
    public NameChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public NameChanged()
    {
    }

    public required string Name { get; set; }
}