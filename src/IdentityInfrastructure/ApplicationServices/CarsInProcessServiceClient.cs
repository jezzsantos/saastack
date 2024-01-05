using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using IdentityApplication;

namespace IdentityInfrastructure.ApplicationServices;

public class IdentityInProcessServiceClient : IIdentityService
{
    private readonly IAPIKeysApplication _apiKeysApplication;

    public IdentityInProcessServiceClient(IAPIKeysApplication apiKeysApplication)
    {
        _apiKeysApplication = apiKeysApplication;
    }

    public async Task<Result<Optional<EndUser>, Error>> FindUserForAPIKeyAsync(ICallerContext caller, string apiKey,
        CancellationToken cancellationToken)
    {
        return await _apiKeysApplication.FindUserForAPIKeyAsync(caller, apiKey, cancellationToken);
    }
}