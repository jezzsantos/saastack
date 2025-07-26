using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native OpenID Connect service for managing and persisting tokens
///     OIDC Specification: <see href="https://openid.net/specs/openid-connect-core-1_0.html" />
/// </summary>
public class NativeIdentityServerOpenIdConnectService : IIdentityServerOpenIdConnectService
{
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IOAuth2ClientService _oauth2ClientService;
    private readonly IRecorder _recorder;

    public NativeIdentityServerOpenIdConnectService(IRecorder recorder, IIdentifierFactory identifierFactory,
        IOAuth2ClientService oauth2ClientService)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _oauth2ClientService = oauth2ClientService;
    }

    public async Task<Result<OidcAuthorizationResponse, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, string responseType, string scope, string? state, string? nonce, string? codeChallenge,
        string? codeChallengeMethod, CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated)
        {
            //TODO: if not redirect to login page?
            return Error.NotAuthenticated();
        }

        var retrievedClient = await _oauth2ClientService.FindClientByIdAsync(caller, clientId, cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        if (!retrievedClient.Value.HasValue)
        {
            return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_InvalidClientId,
                new Dictionary<string, object> { { "code", "invalid_client" } });
        }

        var client = retrievedClient.Value;
        var userId = caller.CallerId;
        var consentedClient =
            await _oauth2ClientService.HasClientConsentedUserAsync(caller, clientId, userId, cancellationToken);
        if (consentedClient.IsFailure)
        {
            return consentedClient.Error;
        }

        if (!consentedClient.Value)
        {
            //TODO: If not consented then redirect to consent page?
            return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_ClientNotConsented,
                new Dictionary<string, object> { { "code", "consent_required" } });
        }

        //Authz code
        //Do all this in the aggregate
        //Validate the request parameters (various rules)
        //Store them as an authorization request (for later).
        //Create a code and return it.
        //To make this idempotent, we need some kind of key to store and retrieve them later, or we are updating the same root instance each time
        //The key would be  shahash256 the following parameters to make this root instance deterministic:
        //CallerId, ClientId, RedirectUri, Scope, Nonce?, CodeChallenge? State?
        //We look that up to get our root instance, and then we extract our unique code from that instance.

        // Create authorization request following OpenIddict pattern
        var request = new OidcAuthorizationRequest
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            ResponseType = responseType,
            Scope = scope,
            State = state,
            Nonce = nonce,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod
        };

        // Validate the authorization request
        var validationResult = ValidateAuthorizationRequest(request);
        if (validationResult.IsFailure)
        {
            return validationResult.Error;
        }

        // Validate redirect URI against registered URIs
        var redirectValidation = ValidateRedirectUri(client, redirectUri);
        if (redirectValidation.IsFailure)
        {
            return redirectValidation.Error;
        }

        // Create authorization code with associated data
        var codeResult = await CreateAuthorizationCodeAsync(caller, client, request, cancellationToken);
        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        return new OidcAuthorizationResponse
        {
            Code = codeResult.Value,
            State = state
        };
    }

    public async Task<Result<OidcTokenResponse, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        string clientId, string clientSecret, string code, string? codeVerifier, string redirectUri,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("ExchangeCodeForTokensAsync not yet implemented");
    }

    public async Task<Result<OidcDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("GetDiscoveryDocumentAsync not yet implemented");
    }

    public async Task<Result<JsonWebKeySet, Error>> GetJsonWebKeySetAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("GetJsonWebKeySetAsync not yet implemented");
    }

    public async Task<Result<OidcUserInfoResponse, Error>> GetUserInfoAsync(ICallerContext caller,
        string userId,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("GetUserInfoForCallerAsync not yet implemented");
    }

    public async Task<Result<OidcTokenResponse, Error>> RefreshTokenAsync(ICallerContext caller, string clientId,
        string clientSecret, string refreshToken, string? scope, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("RefreshTokenAsync not yet implemented");
    }

    private static Result<Error> ValidateAuthorizationRequest(OidcAuthorizationRequest request)
    {
        //Validate the request parameters (various rules)
        // // Validate response type
        // if (request.ResponseType.NotEqualsIgnoreCase(OpenIdConnectConstants.ResponseTypes.Code))
        // {
        //     return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_UnsupportedResponseType);
        // }
        //
        // // Validate scope contains openid
        // var scopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // if (scopes.HasNone() || !scopes.Contains(OpenIdConnectConstants.Scopes.OpenId))
        // {
        //     return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_OpenIdScopeRequired);
        // }
        //
        // // Validate PKCE if code_challenge is provided
        // if (request.CodeChallenge.HasValue())
        // {
        //     if (request.CodeChallengeMethod.HasNoValue())
        //     {
        //         return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_CodeChallengeMethodRequired);
        //     }
        //
        //     if (!OpenIdConnectConstants.CodeChallengeMethods.AllMethods.Contains(request.CodeChallengeMethod))
        //     {
        //         return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_InvalidCodeChallengeMethod);
        //     }
        // }

        return Result.Ok;
    }

    private static Result<Error> ValidateRedirectUri(OAuth2Client client, string redirectUri)
    {
        // TODO: Implement redirect URI validation
        return Result.Ok;
    }

    private static async Task<Result<string, Error>> CreateAuthorizationCodeAsync(ICallerContext caller,
        OAuth2Client client, OidcAuthorizationRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        // TODO: Implement authorization code creation
        return Guid.NewGuid().ToString("N")[..16];
    }

    private class OidcAuthorizationRequest
    {
        public required string ClientId { get; set; }

        public string? CodeChallenge { get; set; }

        public string? CodeChallengeMethod { get; set; }

        public string? Nonce { get; set; }

        public required string RedirectUri { get; set; }

        public required string ResponseType { get; set; }

        public required string Scope { get; set; }

        public string? State { get; set; }
    }

    private class OidcAuthorizationData
    {
        public DateTime ExpiresAt { get; set; }

        public string? Nonce { get; set; }

        public required List<string> Scopes { get; set; }

        public required string Subject { get; set; }
    }

    private class OidcTokenData
    {
        public DateTime ExpiresAt { get; set; }

        public required List<string> Scopes { get; set; }

        public required string Subject { get; set; }
    }
}