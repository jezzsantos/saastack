using Application.Persistence.Common;
using Common;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("PasswordCredential")]
public class PasswordCredential : ReadModelEntity
{
    public Optional<bool> AccountLocked { get; set; }

    public Optional<string> PasswordResetToken { get; set; }

    public Optional<string> RegistrationVerificationToken { get; set; }

    public Optional<bool> RegistrationVerified { get; set; }

    public Optional<string> UserEmailAddress { get; set; }

    public Optional<string> UserId { get; set; }

    public Optional<string> UserName { get; set; }
}