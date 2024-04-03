using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class KeyVerified : DomainEvent
{
    public KeyVerified(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public KeyVerified()
    {
    }

    public required bool IsVerified { get; set; }
}