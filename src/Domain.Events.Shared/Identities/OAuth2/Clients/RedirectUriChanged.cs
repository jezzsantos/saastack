using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.Clients;

public sealed class RedirectUriChanged : DomainEvent
{
    public RedirectUriChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RedirectUriChanged()
    {
    }

    public required string RedirectUri { get; set; }
}