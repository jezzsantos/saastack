using Common;
using Domain.Shared.Identities;

namespace Domain.Services.Shared.DomainServices;

public interface ITokensService
{
    APIKeyToken CreateAPIKey();

    string CreateGuestInvitationToken();

    string CreateJWTRefreshToken();

    string CreatePasswordResetToken();

    string CreateRegistrationVerificationToken();

    Optional<APIKeyToken> ParseApiKey(string apiKey);
}