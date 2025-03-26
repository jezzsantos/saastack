using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IAPIKeysApplication
{
    Task<Result<EndUserWithMemberships, Error>> AuthenticateAsync(ICallerContext caller, string apiKey,
        CancellationToken cancellationToken);

    Task<Result<APIKey, Error>> CreateAPIKeyForCallerAsync(ICallerContext caller, DateTime? expiresOn,
        CancellationToken cancellationToken);

    Task<Result<APIKey, Error>> CreateAPIKeyForUserAsync(ICallerContext caller, string userId, string description,
        DateTime? expiresOn, CancellationToken cancellationToken);

    Task<Result<Error>> DeleteAPIKeyAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<Error>> RevokeAPIKeyAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<SearchResults<APIKey>, Error>> SearchAllAPIKeysForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);
}