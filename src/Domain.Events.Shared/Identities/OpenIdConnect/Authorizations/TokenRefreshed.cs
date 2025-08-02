using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;

public sealed class TokenRefreshed : DomainEvent
{
    public TokenRefreshed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TokenRefreshed()
    {
    }

    public required DateTime RefreshedAt { get; set; }
}