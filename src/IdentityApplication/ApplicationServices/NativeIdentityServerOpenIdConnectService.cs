using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Services.Shared;
using IdentityApplication.Persistence;
using IdentityDomain;
using AuthToken = IdentityDomain.AuthToken;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;
using OAuth2TokenType = Application.Resources.Shared.OAuth2TokenType;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native OpenID Connect service for managing and persisting tokens
///     OIDC Specification: <see href="https://openid.net/specs/openid-connect-core-1_0.html" />
/// </summary>
public class NativeIdentityServerOpenIdConnectService : IIdentityServerOpenIdConnectService
{
    private const string ProviderName = "OpenIdConnect";
    private readonly IAuthTokensService _authTokensService;
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IOAuth2ClientService _oauth2ClientService;
    private readonly IRecorder _recorder;
    private readonly IOidcAuthorizationRepository _repository;
    private readonly ITokensService _tokensService;
    private readonly IUserProfilesService _userProfilesService;
    private readonly IWebsiteUiService _websiteUiService;

    public NativeIdentityServerOpenIdConnectService(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITokensService tokensService, IWebsiteUiService websiteUiService, IOAuth2ClientService oauth2ClientService,
        IAuthTokensService authTokensService, IEndUsersService endUsersService,
        IUserProfilesService userProfilesService, IOidcAuthorizationRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _oauth2ClientService = oauth2ClientService;
        _authTokensService = authTokensService;
        _endUsersService = endUsersService;
        _userProfilesService = userProfilesService;
        _repository = repository;
        _websiteUiService = websiteUiService;
        _tokensService = tokensService;
    }

    public async Task<Result<OpenIdConnectAuthorization, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string userId, string redirectUri, OAuth2ResponseType responseType, string scope, string? state, string? nonce,
        string? codeChallenge, OAuth2CodeChallengeMethod? codeChallengeMethod, CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated)
        {
            return new OpenIdConnectAuthorization
            {
                RawRedirectUri = _websiteUiService.ConstructLoginPageUrl(),
                Code = null
            };
        }

        if (IsAuthorizationResponseTypeUnsupported())
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_Authorize_UnsupportedResponseType.Format(
                    responseType), OAuth2Constants.ErrorCodes.UnsupportedResponseType);
        }

        if (IsOpenIdConnectScopeMissing())
        {
            return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_Authorize_MissingOpenIdScope,
                OAuth2Constants.ErrorCodes.InvalidScope);
        }

        if (IsPkceCodeChallengeMisconfigured())
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_Authorize_MissingCodeChallengeMethod,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        var retrievedClient = await _oauth2ClientService.FindClientByIdAsync(caller, clientId, cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        if (!retrievedClient.Value.HasValue)
        {
            return Error.Validation(Resources.NativeIdentityServerOpenIdConnectService_Authorize_UnknownClient,
                OAuth2Constants.ErrorCodes.InvalidClient);
        }

        var client = retrievedClient.Value.Value;
        if (IsRedirectUriMismatched())
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_Authorize_MismatchedRequestUri.Format(redirectUri),
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        var consented =
            await _oauth2ClientService.HasClientConsentedUserAsync(caller, client.Id, userId, scope, cancellationToken);
        if (consented.IsFailure)
        {
            return consented.Error;
        }

        if (!consented.Value)
        {
            return new OpenIdConnectAuthorization
            {
                RawRedirectUri = _websiteUiService.ConstructOAuth2ConsentPageUrl(),
                Code = null
            };
        }

        var retrievedAuthorization =
            await _repository.FindByClientAndUserAsync(client.Id.ToId(), userId.ToId(), cancellationToken);
        if (retrievedAuthorization.IsFailure)
        {
            return retrievedAuthorization.Error;
        }

        //Fetch authorization
        OidcAuthorizationRoot authorization;
        if (!retrievedAuthorization.Value.HasValue)
        {
            var created = OidcAuthorizationRoot.Create(_recorder, _identifierFactory, _tokensService, clientId.ToId(),
                userId.ToId());
            if (created.IsFailure)
            {
                return created.Error;
            }

            authorization = created.Value;
        }
        else
        {
            authorization = retrievedAuthorization.Value.Value;
        }

        var scopes = OAuth2Scopes.Create(scope);
        if (scopes.IsFailure)
        {
            return scopes.Error;
        }

        var challengeMethod = codeChallengeMethod
                                  ?.ToEnum<OAuth2CodeChallengeMethod,
                                      Domain.Shared.Identities.OAuth2CodeChallengeMethod>().ToOptional()
                              ?? Optional<Domain.Shared.Identities.OAuth2CodeChallengeMethod>.None;
        var authorized =
            authorization.AuthorizeCode(client.RedirectUri, redirectUri, scopes.Value, nonce, codeChallenge,
                challengeMethod);
        if (authorized.IsFailure)
        {
            return authorized.Error;
        }

        var saved = await _repository.SaveAsync(authorization, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        authorization = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Authorization code generated for {UserId} and {ClientId}",
            authorization.UserId, authorization.ClientId);
        _recorder.AuditAgainst(caller.ToCall(), userId,
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_Prepared,
            "Authorization for user {Id} was prepared for client {ClientId} with scopes {Scopes}, from {RedirectUri}, with nonce {Nonce}",
            authorization.UserId, authorization.ClientId, authorization.Scopes.Value.Items.JoinAsOredChoices(),
            authorization.RedirectUri, authorization.Nonce);

        return new OpenIdConnectAuthorization
        {
            RawRedirectUri = null,
            Code = new OpenIdConnectAuthorizationCode
            {
                Code = authorization.AuthorizationCode.Value,
                State = state
            }
        };

        bool IsOpenIdConnectScopeMissing()
        {
            return !scope.Contains(OpenIdConnectConstants.Scopes.OpenId, StringComparison.InvariantCultureIgnoreCase);
        }

        bool IsAuthorizationResponseTypeUnsupported()
        {
            return responseType != OAuth2ResponseType.Code;
        }

        bool IsPkceCodeChallengeMisconfigured()
        {
            return codeChallenge.HasValue() && !codeChallengeMethod.HasValue;
        }

        bool IsRedirectUriMismatched()
        {
            return client.RedirectUri.HasNoValue()
                   || redirectUri.NotEqualsIgnoreCase(client.RedirectUri);
        }
    }

    public async Task<Result<OpenIdConnectTokens, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        string clientId, string clientSecret, string code, string redirectUri, string? codeVerifier,
        CancellationToken cancellationToken)
    {
        var retrievedClient =
            await _oauth2ClientService.VerifyClientAsync(caller, clientId, clientSecret, cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownClient,
                OAuth2Constants.ErrorCodes.InvalidClient);
        }

        var client = retrievedClient.Value;
        var retrievedAuthorization =
            await _repository.FindByAuthorizationCodeAsync(client.Id.ToId(), code, cancellationToken);
        if (retrievedAuthorization.IsFailure)
        {
            return retrievedAuthorization.Error;
        }

        if (!retrievedAuthorization.Value.HasValue)
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownAuthorizationCode,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        EndUserWithMemberships? endUser = null;
        UserProfile? userProfile = null;
        var authorization = retrievedAuthorization.Value.Value;
        var exchanged =
            await authorization.ExchangeCodeAsync(redirectUri, codeVerifier, CreateAccessTokensAsync);
        if (exchanged.IsFailure)
        {
            return exchanged.Error;
        }

        var saved = await _repository.SaveAsync(authorization, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        authorization = saved.Value;
        var userId = authorization.UserId;
        _recorder.TraceInformation(caller.ToCall(), "Authorization granted for {UserId} and {ClientId}",
            userId, authorization.ClientId);
        _recorder.AuditAgainst(caller.ToCall(), userId,
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_Passed,
            "Authorization for user {Id} was requested for client {ClientId} with scopes {Scopes}, from {RedirectUri}, with nonce {Nonce}",
            userId, authorization.ClientId, authorization.Scopes.Value.Items.JoinAsOredChoices(),
            authorization.RedirectUri, authorization.Nonce);
        _recorder.TrackUsageFor(caller.ToCall(), userId,
            UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            endUser!.ToLoginUserUsage(ProviderName, userProfile!));

        var tokens = exchanged.Value;
        return NativeIdentityServerOpenIdConnectServiceConversionExtensions.ToToken(tokens);

        async Task<Result<AuthTokens, Error>> CreateAccessTokensAsync(OidcAuthorizationRoot auth)
        {
            var retrievedUser =
                await _endUsersService.GetMembershipsPrivateAsync(caller, auth.UserId, cancellationToken);
            if (retrievedUser.IsFailure)
            {
                return Error.Validation(
                    Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUser,
                    OAuth2Constants.ErrorCodes.InvalidRequest);
            }

            var maintainer = Caller.CreateAsMaintenance(caller);
            var retrievedProfile =
                await _userProfilesService.GetProfilePrivateAsync(maintainer, auth.UserId, cancellationToken);
            if (retrievedProfile.IsFailure)
            {
                return Error.Validation(
                    Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUserProfile,
                    OAuth2Constants.ErrorCodes.InvalidRequest);
            }

            userProfile = retrievedProfile.Value;
            endUser = retrievedUser.Value;
            var additionalData = new Dictionary<string, object>();
            if (auth.Nonce.HasValue)
            {
                additionalData.Add(AuthenticationConstants.Claims.ForNonce, auth.Nonce);
            }

            var issued =
                await _authTokensService.IssueTokensAsync(caller, endUser, userProfile, auth.Scopes.Value.Items,
                    additionalData,
                    cancellationToken);
            if (issued.IsFailure)
            {
                return issued.Error;
            }

            var accessToken = AuthToken.Create(AuthTokenType.AccessToken, issued.Value.AccessToken,
                issued.Value.AccessTokenExpiresOn);
            var refreshToken = AuthToken.Create(AuthTokenType.RefreshToken, issued.Value.RefreshToken,
                issued.Value.RefreshTokenExpiresOn);
            var idToken = AuthToken.Create(AuthTokenType.OtherToken, issued.Value.IdToken!,
                issued.Value.IdTokenExpiresOn);
            return AuthTokens.Create([accessToken.Value, refreshToken.Value, idToken.Value]);
        }
    }

    public async Task<Result<OpenIdConnectDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
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

    public async Task<Result<OpenIdConnectUserInfo, Error>> GetUserInfoAsync(ICallerContext caller,
        string userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("GetUserInfoForCallerAsync not yet implemented");
    }

    public async Task<Result<OpenIdConnectTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string clientId, string clientSecret, string refreshToken, string? scope, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder implementation
        return Error.Unexpected("RefreshTokenAsync not yet implemented");
    }
}

public static class NativeIdentityServerOpenIdConnectServiceConversionExtensions
{
    public static OpenIdConnectTokens ToToken(AuthTokens tokens)
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var allTokens = tokens.ToList();
        var accessToken = allTokens.Single(tok => tok.Type == AuthTokenType.AccessToken);
        var refreshToken = allTokens.Single(tok => tok.Type == AuthTokenType.RefreshToken);
        var idToken = allTokens.Single(tok => tok.Type == AuthTokenType.OtherToken);
        return new OpenIdConnectTokens
        {
            AccessToken = accessToken.EncryptedValue,
            TokenType = OAuth2TokenType.Bearer,
            ExpiresIn = (int)accessToken.ExpiresOn!.Value.Subtract(now).TotalSeconds,
            RefreshToken = refreshToken.EncryptedValue,
            IdToken = idToken.EncryptedValue
        };
    }
}