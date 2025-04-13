using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

#pragma warning disable SAASDDD043
public sealed class MfaStateReset : DomainEvent
#pragma warning restore SAASDDD043
{
    public MfaStateReset(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaStateReset()
    {
    }

    public required bool CanBeDisabled { get; set; }

    public required bool IsEnabled { get; set; }

    public required string UserId { get; set; }
}