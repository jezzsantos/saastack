using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class Deleted : TombstoneDomainEvent
{
    public Deleted(Identifier id, Identifier deletedById) : base(id, deletedById)
    {
    }

    [UsedImplicitly]
    public Deleted()
    {
    }
}