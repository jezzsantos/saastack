using Common;
using Domain.Shared.Identities;

namespace Domain.Services.Shared;

public interface ITokensService
{
    APIKeyToken CreateAPIKey();

    string CreateGuestInvitationToken();

    string CreateJWTRefreshToken();

    string CreateMfaAuthenticationToken();

    string CreatePasswordResetToken();

    string CreateRegistrationVerificationToken();

    string GenerateRandomToken();

    Optional<APIKeyToken> ParseApiKey(string apiKey);

    string CreateOAuth2ClientSecret();

    string CreateOAuthorizationCodeDigest(string authorizationCode);

    /// <summary>
    ///     Creates a deterministic digest of the specified token
    /// </summary>
    string CreateTokenDigest(string token);
}