using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IdentityApplication;

public class SingleSignOnApplication : ISingleSignOnApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public SingleSignOnApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller,
        string? invitationToken, string providerName, string authCode, string? codeVerifier, string? username,
        bool? termsAndConditionsAccepted, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.SingleSignOnService.AuthenticateAsync(caller, invitationToken,
            providerName, authCode, codeVerifier, username, termsAndConditionsAccepted, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.SingleSignOnService.GetTokensForUserAsync(caller, caller.ToCallerId(),
            cancellationToken);
    }

    public async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensOnBehalfOfUserAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.SingleSignOnService.GetTokensForUserAsync(caller, userId,
            cancellationToken);
    }

    public async Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string providerName, string refreshToken, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.SingleSignOnService.RefreshTokenForUserAsync(caller, caller.ToCallerId(),
            providerName, refreshToken,
            cancellationToken);
    }

    public async Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenOnBehalfOfUserAsync(
        ICallerContext caller, string userId, string providerName, string refreshToken,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.SingleSignOnService.RefreshTokenForUserAsync(caller, userId, providerName,
            refreshToken,
            cancellationToken);
    }
}