using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface ISSOService
{
    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller, string userId,
        string providerName, string refreshToken, CancellationToken cancellationToken);
}