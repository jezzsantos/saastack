using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class RegistrationVerificationVerified : DomainEvent
{
    public RegistrationVerificationVerified(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RegistrationVerificationVerified()
    {
    }
}