using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface ISingleSignOnApplication
{
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string? invitationToken,
        string providerName, string authCode, string? username, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller, string userId,
        string providerName,
        string refreshToken, CancellationToken cancellationToken);
}