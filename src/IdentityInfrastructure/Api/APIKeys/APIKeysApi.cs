using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.APIKeys;

public class APIKeysApi : IWebApiService
{
    private readonly IAPIKeysApplication _apiKeysApplication;
    private readonly ICallerContextFactory _callerFactory;

    public APIKeysApi(ICallerContextFactory callerFactory, IAPIKeysApplication apiKeysApplication)
    {
        _callerFactory = callerFactory;
        _apiKeysApplication = apiKeysApplication;
    }

    public async Task<ApiPostResult<APIKey, CreateAPIKeyResponse>> CreateAPIKey(
        CreateAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var apiKey =
            await _apiKeysApplication.CreateAPIKeyForCallerAsync(_callerFactory.Create(), request.ExpiresOnUtc,
                cancellationToken);

        return () => apiKey.HandleApplicationResult<APIKey, CreateAPIKeyResponse>(x =>
            new PostResult<CreateAPIKeyResponse>(new CreateAPIKeyResponse { ApiKey = x.Key }));
    }

    public async Task<ApiDeleteResult> DeleteAPIKey(DeleteAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var key = await _apiKeysApplication.DeleteAPIKeyAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => key.HandleApplicationResult();
    }

    public async Task<ApiDeleteResult> RevokeAPIKey(RevokeAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var key = await _apiKeysApplication.RevokeAPIKeyAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => key.HandleApplicationResult();
    }

    public async Task<ApiSearchResult<APIKey, SearchAllAPIKeysResponse>> SearchAllAPIKeysForCaller(
        SearchAllAPIKeysForCallerRequest request, CancellationToken cancellationToken)
    {
        var keys = await _apiKeysApplication.SearchAllAPIKeysForCallerAsync(_callerFactory.Create(),
            request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () => keys.HandleApplicationResult(x => new SearchAllAPIKeysResponse
            { Keys = x.Results, Metadata = x.Metadata });
    }
}