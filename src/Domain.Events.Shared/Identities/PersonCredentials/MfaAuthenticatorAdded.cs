using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Identities;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class MfaAuthenticatorAdded : DomainEvent
{
    public MfaAuthenticatorAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaAuthenticatorAdded()
    {
    }

    public string? AuthenticatorId { get; set; }

    public required bool IsActive { get; set; }

    public required MfaAuthenticatorType Type { get; set; }

    public required string UserId { get; set; }
}