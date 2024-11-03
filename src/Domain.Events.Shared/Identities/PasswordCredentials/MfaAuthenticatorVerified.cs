using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Identities;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PasswordCredentials;

public sealed class MfaAuthenticatorVerified : DomainEvent
{
    public MfaAuthenticatorVerified(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaAuthenticatorVerified()
    {
    }

    public required string AuthenticatorId { get; set; }

    public string? ConfirmationCode { get; set; }

    public string? VerifiedState { get; set; }

    public string? OobCode { get; set; }

    public required MfaAuthenticatorType Type { get; set; }

    public required string UserId { get; set; }
}