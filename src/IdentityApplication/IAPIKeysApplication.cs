using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IAPIKeysApplication
{
    Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext context, string userId, string description,
        DateTime? expiresOn, CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext context, CancellationToken cancellationToken);
#endif
    Task<Result<Optional<EndUser>, Error>> FindUserForAPIKeyAsync(ICallerContext context, string apiKey,
        CancellationToken cancellationToken);
}