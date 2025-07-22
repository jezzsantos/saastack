using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for managing APIKeys for an identity server
/// </summary>
public interface IIdentityServerApiKeyService
{
    /// <summary>
    ///     Authenticates the caller using the specified API Key
    /// </summary>
    Task<Result<EndUserWithMemberships, Error>> AuthenticateAsync(ICallerContext caller, string apiKey,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a new API Key for the specified user
    /// </summary>
    Task<Result<APIKey, Error>> CreateAPIKeyForUserAsync(ICallerContext caller, string userId, string description,
        DateTime? expiresOn, CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the specified API Key for the specified user
    /// </summary>
    Task<Result<Error>> DeleteAPIKeyForUserAsync(ICallerContext caller, string id, string userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Revokes the specified API Key
    /// </summary>
    Task<Result<Error>> RevokeAPIKeyAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    /// <summary>
    ///     Searches for all API Keys for the specified user
    /// </summary>
    Task<Result<SearchResults<APIKey>, Error>> SearchAllAPIKeysForUserAsync(ICallerContext caller, string userId,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);
}