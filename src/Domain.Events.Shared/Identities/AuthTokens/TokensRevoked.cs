using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.AuthTokens;

public sealed class TokensRevoked : DomainEvent
{
    public TokensRevoked(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TokensRevoked()
    {
    }

    public required string UserId { get; set; }
}