using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class PasswordResetInitiated : DomainEvent
{
    public PasswordResetInitiated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PasswordResetInitiated()
    {
    }

    public required string Token { get; set; }
}