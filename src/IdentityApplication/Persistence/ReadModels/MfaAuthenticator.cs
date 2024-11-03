using Application.Persistence.Common;
using Common;
using Domain.Shared.Identities;
using IdentityDomain;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("MfaAuthenticator")]
public class MfaAuthenticator : ReadModelEntity
{
    public Optional<string> BarCodeUri { get; set; }

    public Optional<string> VerifiedState { get; set; }

    public bool IsActive { get; set; }

    public Optional<string> OobChannelValue { get; set; }

    public Optional<string> OobCode { get; set; }

    public Optional<string> PasswordCredentialId { get; set; }

    public Optional<string> Secret { get; set; }

    public MfaAuthenticatorState State { get; set; }

    public MfaAuthenticatorType Type { get; set; } = MfaAuthenticatorType.None;

    public Optional<string> UserId { get; set; }
}