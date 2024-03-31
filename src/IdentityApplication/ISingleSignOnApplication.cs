using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface ISingleSignOnApplication
{
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext context, string? invitationToken,
        string providerName,
        string authCode, string? username, CancellationToken cancellationToken);
}