using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IAPIKeysApplication
{
    Task<Result<EndUserWithMemberships, Error>> AuthenticateAsync(ICallerContext caller, string apiKey,
        CancellationToken cancellationToken);

    Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext caller, string userId, string description,
        DateTime? expiresOn, CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<APIKey, Error>> CreateAPIKeyForCallerAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

    Task<Result<Error>> DeleteAPIKeyAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<SearchResults<APIKey>, Error>> SearchAllAPIKeysForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);
}