using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IOAuth2ClientApplication
{
    Task<Result<bool, Error>> ConsentToClientAsync(ICallerContext caller, string clientId,
        string? scope, bool consented, CancellationToken cancellationToken);

    Task<Result<OAuth2Client, Error>> CreateClientAsync(ICallerContext caller, string name, string? redirectUri,
        CancellationToken cancellationToken);

    Task<Result<Error>> DeleteClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<OAuth2Client, Error>> GetClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> GetConsentAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken);

    Task<Result<OAuth2ClientWithSecret, Error>> RegenerateClientSecretAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<Error>> RevokeConsentAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<OAuth2Client>, Error>> SearchAllClientsAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<OAuth2Client, Error>> UpdateClientAsync(ICallerContext caller, string id, string? name,
        string? redirectUri, CancellationToken cancellationToken);
}