using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Identities;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class MfaAuthenticatorRemoved : DomainEvent
{
    public MfaAuthenticatorRemoved(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaAuthenticatorRemoved()
    {
    }

    public required string AuthenticatorId { get; set; }

    public required MfaAuthenticatorType Type { get; set; }

    public required string UserId { get; set; }
}