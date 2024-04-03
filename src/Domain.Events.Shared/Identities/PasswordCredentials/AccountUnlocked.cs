using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class AccountUnlocked : DomainEvent
{
    public AccountUnlocked(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public AccountUnlocked()
    {
    }
}