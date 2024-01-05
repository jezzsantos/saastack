using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IAPIKeysService
{
    Task<Result<APIKey, Error>> CreateApiKeyAsync(ICallerContext context, string userId, string description,
        DateTime? expiresOn,
        CancellationToken cancellationToken);
}