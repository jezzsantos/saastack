using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.Clients;

public sealed class SecretAdded : DomainEvent
{
    public SecretAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SecretAdded()
    {
    }

    public required string SecretHash { get; set; }

    public DateTime? ExpiresOn { get; set; }
}