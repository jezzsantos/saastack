using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class EmailAddressChanged : DomainEvent
{
    public EmailAddressChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public EmailAddressChanged()
    {
    }

    public required string EmailAddress { get; set; }

    public required string UserId { get; set; }
}