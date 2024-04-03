using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class PhoneNumberChanged : DomainEvent
{
    public PhoneNumberChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PhoneNumberChanged()
    {
    }

    public required string Number { get; set; }

    public required string UserId { get; set; }
}