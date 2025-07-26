using Application.Interfaces;
using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class ClientsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IOAuth2ClientApplication _oauth2ClientApplication;

    public ClientsApi(ICallerContextFactory callerFactory, IOAuth2ClientApplication oauth2ClientApplication)
    {
        _callerFactory = callerFactory;
        _oauth2ClientApplication = oauth2ClientApplication;
    }

    public async Task<ApiPostResult<bool, GetOAuth2ClientConsentResponse>> ConsentClientForCaller(
        ConsentToOAuth2ClientForCallerRequest request, CancellationToken cancellationToken)
    {
        var consented = await _oauth2ClientApplication.ConsentToClientAsync(
            _callerFactory.Create(),
            request.Id!,
            request.Scope,
            request.Consented,
            cancellationToken);

        return () => consented.HandleApplicationResult<bool, GetOAuth2ClientConsentResponse>(con =>
            new PostResult<GetOAuth2ClientConsentResponse>(new GetOAuth2ClientConsentResponse { Consented = con }));
    }

    public async Task<ApiPostResult<OAuth2Client, GetOAuth2ClientResponse>> CreateClient(
        CreateOAuth2ClientRequest request, CancellationToken cancellationToken)
    {
        var client =
            await _oauth2ClientApplication.CreateClientAsync(_callerFactory.Create(), request.Name!,
                request.RedirectUri, cancellationToken);

        return () => client.HandleApplicationResult<OAuth2Client, GetOAuth2ClientResponse>(c =>
            new PostResult<GetOAuth2ClientResponse>(new GetOAuth2ClientResponse { Client = c }));
    }

    public async Task<ApiDeleteResult> DeleteClient(
        DeleteOAuth2ClientRequest request, CancellationToken cancellationToken)
    {
        var deleted = await _oauth2ClientApplication.DeleteClientAsync(
            _callerFactory.Create(),
            request.Id!,
            cancellationToken);

        return () => deleted.HandleApplicationResult();
    }

    public async Task<ApiGetResult<OAuth2Client, GetOAuth2ClientResponse>> GetClient(
        GetOAuth2ClientRequest request, CancellationToken cancellationToken)
    {
        var client = await _oauth2ClientApplication.GetClientAsync(
            _callerFactory.Create(),
            request.Id!,
            cancellationToken);

        return () => client.HandleApplicationResult<OAuth2Client, GetOAuth2ClientResponse>(c =>
            new GetOAuth2ClientResponse { Client = c });
    }

    public async Task<ApiGetResult<bool, GetOAuth2ClientConsentResponse>> GetClientConsentForCaller(
        GetOAuth2ClientConsentForCallerRequest request, CancellationToken cancellationToken)
    {
        var consent = await _oauth2ClientApplication.GetConsentAsync(
            _callerFactory.Create(),
            request.Id!,
            cancellationToken);

        return () =>
            consent.HandleApplicationResult<bool, GetOAuth2ClientConsentResponse>(c =>
                new GetOAuth2ClientConsentResponse
                    { Consented = c });
    }

    public async Task<ApiPostResult<OAuth2ClientWithSecret, RegenerateOAuth2ClientSecretResponse>>
        RegenerateClientSecret(RegenerateOAuth2ClientSecretRequest request, CancellationToken cancellationToken)
    {
        var client = await _oauth2ClientApplication.RegenerateClientSecretAsync(
            _callerFactory.Create(),
            request.Id!,
            cancellationToken);

        return () => client.HandleApplicationResult<OAuth2ClientWithSecret, RegenerateOAuth2ClientSecretResponse>(c =>
            new PostResult<RegenerateOAuth2ClientSecretResponse>(
                new RegenerateOAuth2ClientSecretResponse { Client = c }));
    }

    public async Task<ApiDeleteResult> RevokeClientConsentForCaller(
        RevokeOAuth2ClientConsentForCallerRequest request, CancellationToken cancellationToken)
    {
        var result = await _oauth2ClientApplication.RevokeConsentAsync(
            _callerFactory.Create(),
            request.Id!,
            cancellationToken);

        return () => result.HandleApplicationResult();
    }

    public async Task<ApiSearchResult<SearchResults<OAuth2Client>, SearchAllOAuth2ClientsResponse>> SearchAllClients(
        SearchAllOAuth2ClientsRequest request, CancellationToken cancellationToken)
    {
        var clients = await _oauth2ClientApplication.SearchAllClientsAsync(
            _callerFactory.Create(), request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () => clients.HandleApplicationResult<SearchResults<OAuth2Client>, SearchAllOAuth2ClientsResponse>(
            c => new SearchAllOAuth2ClientsResponse
            {
                Clients = c.Results,
                Metadata = c.Metadata
            });
    }

    public async Task<ApiPutPatchResult<OAuth2Client, GetOAuth2ClientResponse>> UpdateClient(
        UpdateOAuth2ClientRequest request, CancellationToken cancellationToken)
    {
        var client = await _oauth2ClientApplication.UpdateClientAsync(_callerFactory.Create(), request.Id!,
            request.Name, request.RedirectUri, cancellationToken);

        return () => client.HandleApplicationResult<OAuth2Client, GetOAuth2ClientResponse>(c =>
            new GetOAuth2ClientResponse { Client = c });
    }
}