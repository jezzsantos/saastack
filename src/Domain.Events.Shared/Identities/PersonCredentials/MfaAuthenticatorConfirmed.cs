using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Identities;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class MfaAuthenticatorConfirmed : DomainEvent
{
    public MfaAuthenticatorConfirmed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MfaAuthenticatorConfirmed()
    {
    }

    public required string AuthenticatorId { get; set; }

    public string? ConfirmationCode { get; set; }

    public required bool IsActive { get; set; }

    public string? OobCode { get; set; }

    public required MfaAuthenticatorType Type { get; set; }

    public required string UserId { get; set; }

    public string? VerifiedState { get; set; }
}