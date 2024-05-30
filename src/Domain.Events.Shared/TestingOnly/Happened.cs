#if TESTINGONLY
using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.TestingOnly;

public sealed class Happened : DomainEvent
{
    public Happened(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Happened()
    {
    }
}
#endif