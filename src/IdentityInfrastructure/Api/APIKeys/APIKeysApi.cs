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
    private readonly ICallerContextFactory _contextFactory;

    public APIKeysApi(ICallerContextFactory contextFactory, IAPIKeysApplication apiKeysApplication)
    {
        _contextFactory = contextFactory;
        _apiKeysApplication = apiKeysApplication;
    }

#if TESTINGONLY
    public async Task<ApiPostResult<APIKey, CreateAPIKeyResponse>> Create(
        CreateAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeysApplication.CreateAPIKeyAsync(_contextFactory.Create(), cancellationToken);

        return () => apiKey.HandleApplicationResult<APIKey, CreateAPIKeyResponse>(x =>
            new PostResult<CreateAPIKeyResponse>(new CreateAPIKeyResponse { ApiKey = x.Key }));
    }
#endif

    public async Task<ApiDeleteResult> DeleteAPIKey(DeleteAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var key = await _apiKeysApplication.DeleteAPIKeyAsync(_contextFactory.Create(), request.Id, cancellationToken);

        return () => key.HandleApplicationResult();
    }

    public async Task<ApiSearchResult<APIKey, SearchAllAPIKeysResponse>> SearchAllAPIKeys(
        SearchAllAPIKeysRequest request, CancellationToken cancellationToken)
    {
        var keys = await _apiKeysApplication.SearchAllAPIKeysAsync(_contextFactory.Create(), request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () => keys.HandleApplicationResult(x => new SearchAllAPIKeysResponse
            { Keys = x.Results, Metadata = x.Metadata });
    }
}