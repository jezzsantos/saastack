using Application.Persistence.Common;
using Common;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("AuthToken")]
public class AuthToken : SnapshottedReadModelEntity
{
    public Optional<string> AccessToken { get; set; }

    public Optional<DateTime> AccessTokenExpiresOn { get; set; }

    public Optional<string> IdToken { get; set; }

    public Optional<DateTime> IdTokenExpiresOn { get; set; }

    public Optional<string> RefreshToken { get; set; }

    public Optional<string> RefreshTokenDigest { get; set; }

    public Optional<DateTime> RefreshTokenExpiresOn { get; set; }

    public Optional<string> UserId { get; set; }
}