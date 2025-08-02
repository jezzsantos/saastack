using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for managing OAuth2 clients for an identity server
/// </summary>
public interface IIdentityServerOAuth2ClientService
{
    /// <summary>
    ///     Consents the user to the OAuth2 client
    /// </summary>
    Task<Result<OAuth2ClientConsent, Error>> ConsentToClientAsync(ICallerContext caller, string clientId, string userId,
        string? scope, bool isConsented, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a new OAuth2 client
    /// </summary>
    Task<Result<OAuth2Client, Error>> CreateClientAsync(ICallerContext caller, string name, string? redirectUri,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the OAuth2 client
    /// </summary>
    Task<Result<Error>> DeleteClientAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    /// <summary>
    ///     Finds the OAuth2 client
    /// </summary>
    Task<Result<Optional<OAuth2Client>, Error>> FindClientByIdAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the OAuth2 client
    /// </summary>
    Task<Result<OAuth2ClientWithSecrets, Error>> GetClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the consent for the user to the OAuth2 client
    /// </summary>
    Task<Result<OAuth2ClientConsent, Error>> GetConsentAsync(ICallerContext caller, string clientId, string userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the user has consented to the OAuth2 client for the specified scope
    /// </summary>
    Task<Result<bool, Error>> HasClientConsentedUserAsync(ICallerContext caller, string clientId, string userId,
        string scope, CancellationToken cancellationToken);

    /// <summary>
    ///     Regenerates a client secret for the OAuth2 client
    /// </summary>
    Task<Result<OAuth2ClientWithSecret, Error>> RegenerateClientSecretAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Revokes the user's consent for the OAuth2 client
    /// </summary>
    Task<Result<Error>> RevokeConsentAsync(ICallerContext caller, string clientId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Lists all OAuth2 clients
    /// </summary>
    Task<Result<SearchResults<OAuth2Client>, Error>> SearchAllClientsAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates the OAuth2 client
    /// </summary>
    Task<Result<OAuth2Client, Error>> UpdateClientAsync(ICallerContext caller, string id, string? name,
        string? redirectUri, CancellationToken cancellationToken);

    /// <summary>
    ///     Verifies the specified client credentials
    /// </summary>
    Task<Result<OAuth2Client, Error>> VerifyClientAsync(ICallerContext caller, string id, string clientSecret,
        CancellationToken cancellationToken);
}