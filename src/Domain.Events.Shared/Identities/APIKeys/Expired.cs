using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class Expired : DomainEvent
{
    public Expired(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Expired()
    {
    }

    public required string UserId { get; set; }

    public required DateTime ExpiredOn { get; set; }
}