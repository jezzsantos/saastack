using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using IdentityApplication;

namespace IdentityInfrastructure.ApplicationServices;

public class SSOInProcessServiceClient : ISSOService
{
    private readonly ISingleSignOnApplication _singleSignOnApplication;

    public SSOInProcessServiceClient(ISingleSignOnApplication singleSignOnApplication)
    {
        _singleSignOnApplication = singleSignOnApplication;
    }

    public Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return _singleSignOnApplication.GetTokensAsync(caller, cancellationToken);
    }

    public Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensOnBehalfOfUserAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        return _singleSignOnApplication.GetTokensOnBehalfOfUserAsync(caller, userId, cancellationToken);
    }

    public Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string providerName, string refreshToken, CancellationToken cancellationToken)
    {
        return _singleSignOnApplication.RefreshTokenAsync(caller, providerName, refreshToken,
            cancellationToken);
    }

    public Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenOnBehalfOfUserAsync(ICallerContext caller,
        string userId, string providerName, string refreshToken, CancellationToken cancellationToken)
    {
        return _singleSignOnApplication.RefreshTokenOnBehalfOfUserAsync(caller, userId, providerName, refreshToken,
            cancellationToken);
    }
}