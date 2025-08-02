using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;

public sealed class CodeExchanged : DomainEvent
{
    public CodeExchanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public CodeExchanged()
    {
    }

    public required DateTime ExchangedAt { get; set; }
}