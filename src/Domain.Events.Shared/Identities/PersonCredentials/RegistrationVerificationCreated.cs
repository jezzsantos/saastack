using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class RegistrationVerificationCreated : DomainEvent
{
    public RegistrationVerificationCreated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RegistrationVerificationCreated()
    {
    }

    public required string Token { get; set; }
}