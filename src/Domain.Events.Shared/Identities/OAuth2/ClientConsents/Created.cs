using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.ClientConsents;

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

    public required bool IsConsented { get; set; }

    public required List<string> Scopes { get; set; }

    public required string UserId { get; set; }
}