using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IIdentityService
{
    Task<Result<EndUserWithMemberships, Error>> AuthenticateApiKeyAsync(ICallerContext caller,
        string apiKey, CancellationToken cancellationToken);
}