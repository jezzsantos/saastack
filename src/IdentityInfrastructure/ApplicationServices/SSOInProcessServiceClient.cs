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
        string userId,
        CancellationToken cancellationToken)
    {
        return _singleSignOnApplication.GetTokensAsync(caller, userId, cancellationToken);
    }

    public Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller, string userId,
        string providerName, string refreshToken, CancellationToken cancellationToken)
    {
        return _singleSignOnApplication.RefreshTokenAsync(caller, userId, providerName, refreshToken,
            cancellationToken);
    }
}