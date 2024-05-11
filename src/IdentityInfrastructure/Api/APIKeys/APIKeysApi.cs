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

#if TESTINGONLY
    public async Task<ApiPostResult<APIKey, CreateAPIKeyResponse>> Create(
        CreateAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeysApplication.CreateAPIKeyAsync(_callerFactory.Create(), cancellationToken);

        return () => apiKey.HandleApplicationResult<APIKey, CreateAPIKeyResponse>(x =>
            new PostResult<CreateAPIKeyResponse>(new CreateAPIKeyResponse { ApiKey = x.Key }));
    }
#endif

    public async Task<ApiDeleteResult> DeleteAPIKey(DeleteAPIKeyRequest request, CancellationToken cancellationToken)
    {
        var key = await _apiKeysApplication.DeleteAPIKeyAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => key.HandleApplicationResult();
    }

    public async Task<ApiSearchResult<APIKey, SearchAllAPIKeysResponse>> SearchAllAPIKeys(
        SearchAllAPIKeysRequest request, CancellationToken cancellationToken)
    {
        var keys = await _apiKeysApplication.SearchAllAPIKeysAsync(_callerFactory.Create(), request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () => keys.HandleApplicationResult(x => new SearchAllAPIKeysResponse
            { Keys = x.Results, Metadata = x.Metadata });
    }
}