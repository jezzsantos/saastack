using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class MfaAuthenticationInitiated : DomainEvent
{
    public MfaAuthenticationInitiated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaAuthenticationInitiated()
    {
    }

    public required DateTime AuthenticationExpiresAt { get; set; }

    public required string AuthenticationToken { get; set; }

    public required string UserId { get; set; }
}