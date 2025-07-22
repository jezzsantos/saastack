using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OpenIdConnect;

public class OpenIdConnectApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IOpenIdConnectApplication _openIdConnectApplication;

    public OpenIdConnectApi(ICallerContextFactory callerFactory, IOpenIdConnectApplication openIdConnectApplication)
    {
        _callerFactory = callerFactory;
        _openIdConnectApplication = openIdConnectApplication;
    }

    public async Task<ApiGetResult<OidcDiscoveryDocument, GetDiscoveryDocumentResponse>> GetDiscoveryDocument(
        GetDiscoveryDocumentRequest _, CancellationToken cancellationToken)
    {
        var result =
            await _openIdConnectApplication.GetDiscoveryDocumentAsync(_callerFactory.Create(), cancellationToken);

        return () => result.HandleApplicationResult<OidcDiscoveryDocument, GetDiscoveryDocumentResponse>(doc =>
            new GetDiscoveryDocumentResponse { Document = doc });
    }

    public async Task<ApiGetResult<JsonWebKeySet, GetJsonWebKeySetResponse>> GetJsonWebKeySet(
        GetJsonWebKeySetRequest _, CancellationToken cancellationToken)
    {
        var result = await _openIdConnectApplication.GetJsonWebKeySetAsync(_callerFactory.Create(), cancellationToken);

        return () => result.HandleApplicationResult<JsonWebKeySet, GetJsonWebKeySetResponse>(jwks =>
            new GetJsonWebKeySetResponse { Keys = jwks });
    }
}