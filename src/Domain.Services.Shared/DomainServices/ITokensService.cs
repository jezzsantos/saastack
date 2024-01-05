using Common;
using Domain.Shared;

namespace Domain.Services.Shared.DomainServices;

public interface ITokensService
{
    APIKeyToken CreateApiKey();

    string CreateTokenForJwtRefresh();

    string CreateTokenForPasswordReset();

    string CreateTokenForVerification();

    Optional<APIKeyToken> ParseApiKey(string apiKey);
}