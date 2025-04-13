using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class MfaOptionsChanged : DomainEvent
{
    public MfaOptionsChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaOptionsChanged()
    {
    }

    public required bool CanBeDisabled { get; set; }

    public required bool IsEnabled { get; set; }

    public required string UserId { get; set; }
}