using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IdentityApplication;

public class APIKeysApplication : IAPIKeysApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public APIKeysApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<EndUserWithMemberships, Error>> AuthenticateAsync(
        ICallerContext caller, string apiKey, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.ApiKeyService.AuthenticateAsync(caller, apiKey, cancellationToken);
    }

    public async Task<Result<APIKey, Error>> CreateAPIKeyForCallerAsync(ICallerContext caller, DateTime? expiresOn,
        CancellationToken cancellationToken)
    {
        return await CreateAPIKeyForUserAsync(caller, caller.CallerId, caller.CallerId, expiresOn, cancellationToken);
    }

    public async Task<Result<APIKey, Error>> CreateAPIKeyForUserAsync(ICallerContext caller, string userId,
        string description, DateTime? expiresOn, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.ApiKeyService.CreateAPIKeyForUserAsync(caller, userId, description,
            expiresOn, cancellationToken);
    }

    public async Task<Result<Error>> DeleteAPIKeyAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.ApiKeyService.DeleteAPIKeyForUserAsync(caller, id, caller.ToCallerId(),
            cancellationToken);
    }

    public async Task<Result<Error>> RevokeAPIKeyAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.ApiKeyService.RevokeAPIKeyAsync(caller, id, cancellationToken);
    }

    public async Task<Result<SearchResults<APIKey>, Error>> SearchAllAPIKeysForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var userId = caller.ToCallerId();
        return await _identityServerProvider.ApiKeyService.SearchAllAPIKeysForUserAsync(caller, userId, searchOptions,
            getOptions, cancellationToken);
    }
}