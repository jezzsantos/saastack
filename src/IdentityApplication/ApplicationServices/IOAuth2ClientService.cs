using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a service for managing OAuth2 clients
/// </summary>
public interface IOAuth2ClientService
{
    Task<Result<Optional<OAuth2Client>, Error>> FindClientByIdAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> HasClientConsentedUserAsync(ICallerContext caller, string clientId, string userId,
        CancellationToken cancellationToken);
}