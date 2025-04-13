using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class RegistrationChanged : DomainEvent
{
    public RegistrationChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RegistrationChanged()
    {
    }

    public required string EmailAddress { get; set; }

    public required string Name { get; set; }
}