using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

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