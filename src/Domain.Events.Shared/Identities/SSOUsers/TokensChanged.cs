using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.SSOUsers;

public sealed class TokensChanged : DomainEvent
{
    public TokensChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TokensChanged()
    {
    }

    public required List<SSOToken> Tokens { get; set; }
}

public class SSOToken
{
    public required string EncryptedValue { get; set; }

    public DateTime? ExpiresOn { get; set; }

    public required string Type { get; set; }
}