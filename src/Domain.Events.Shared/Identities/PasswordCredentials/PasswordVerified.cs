using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class PasswordVerified : DomainEvent
{
    public PasswordVerified(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PasswordVerified()
    {
    }

    public required bool AuditAttempt { get; set; }

    public required bool IsVerified { get; set; }
}