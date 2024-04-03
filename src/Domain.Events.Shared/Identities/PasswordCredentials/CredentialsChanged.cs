using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class CredentialsChanged : DomainEvent
{
    public CredentialsChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public CredentialsChanged()
    {
    }

    public required string PasswordHash { get; set; }
}