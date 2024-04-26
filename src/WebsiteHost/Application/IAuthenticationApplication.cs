using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace WebsiteHost.Application;

public interface IAuthenticationApplication
{
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string provider,
        string? authCode, string? username, string? password, CancellationToken cancellationToken);

    Task<Result<Error>> LogoutAsync(ICallerContext caller, CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext caller, string? refreshToken,
        CancellationToken cancellationToken);
}