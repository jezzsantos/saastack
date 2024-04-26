using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IAPIKeysApplication
{
    Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext caller, string userId, string description,
        DateTime? expiresOn, CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

    Task<Result<Error>> DeleteAPIKeyAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<Optional<EndUserWithMemberships>, Error>> FindMembershipsForAPIKeyAsync(ICallerContext caller,
        string apiKey,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<APIKey>, Error>> SearchAllAPIKeysAsync(ICallerContext caller, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);
}