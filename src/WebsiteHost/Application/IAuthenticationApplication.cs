using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace WebsiteHost.Application;

public interface IAuthenticationApplication
{
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext context, string provider,
        string? authCode, string? username, string? password, CancellationToken cancellationToken);

    Task<Result<Error>> LogoutAsync(ICallerContext context, CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext context, string? refreshToken,
        CancellationToken cancellationToken);
}