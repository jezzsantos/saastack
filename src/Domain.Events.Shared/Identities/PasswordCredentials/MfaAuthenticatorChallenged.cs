using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Identities;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class MfaAuthenticatorChallenged : DomainEvent
{
    public MfaAuthenticatorChallenged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaAuthenticatorChallenged()
    {
    }

    public required string AuthenticatorId { get; set; }

    public string? BarCodeUri { get; set; }

    public string? OobChannelValue { get; set; }

    public string? OobCode { get; set; }

    public string? Secret { get; set; }

    public required MfaAuthenticatorType Type { get; set; }

    public required string UserId { get; set; }
}