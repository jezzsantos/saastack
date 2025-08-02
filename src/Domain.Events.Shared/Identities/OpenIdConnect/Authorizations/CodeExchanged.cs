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

    public required string AccessTokenDigest { get; set; }

    public DateTime? AccessTokenExpiresOn { get; set; }

    public required DateTime ExchangedAt { get; set; }

    public required string RefreshTokenDigest { get; set; }

    public DateTime? RefreshTokenExpiresOn { get; set; }
}