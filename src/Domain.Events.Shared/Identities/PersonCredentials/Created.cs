using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required bool IsMfaEnabled { get; set; }

    public required bool MfaCanBeDisabled { get; set; }

    public required string UserId { get; set; }
}