using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.ClientConsents;

public sealed class ConsentChanged : DomainEvent
{
    public ConsentChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ConsentChanged()
    {
    }

    public required bool IsConsented { get; set; }

    public required List<string> Scopes { get; set; }
}