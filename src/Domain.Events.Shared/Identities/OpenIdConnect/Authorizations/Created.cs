using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string ClientId { get; set; }

    public required string UserId { get; set; }
}