using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class PasswordResetCompleted : DomainEvent
{
    public PasswordResetCompleted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PasswordResetCompleted()
    {
    }

    public required string PasswordHash { get; set; }

    public required string Token { get; set; }
}