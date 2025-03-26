using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class Revoked : DomainEvent
{
    public Revoked(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Revoked()
    {
    }

    public required DateTime RevokedOn { get; set; }

    public required string UserId { get; set; }
}