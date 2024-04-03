using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class AccountLocked : DomainEvent
{
    public AccountLocked(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public AccountLocked()
    {
    }
}