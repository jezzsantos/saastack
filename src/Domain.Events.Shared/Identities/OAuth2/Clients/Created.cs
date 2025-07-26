using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.Clients;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string Name { get; set; }
}