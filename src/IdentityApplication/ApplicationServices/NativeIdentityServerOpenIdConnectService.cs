using System.Security.Cryptography;
using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Resources.Shared.Extensions;
using Application.Services.Shared;
using Common;
using Common.Configuration;
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
using OpenIdConnectCodeChallengeMethod = Domain.Shared.Identities.OpenIdConnectCodeChallengeMethod;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native OpenID Connect service for managing and persisting tokens
///     OIDC Specification: <see href="https://openid.net/specs/openid-connect-core-1_0.html" />
///     Discovery Specification: <see href="https://openid.net/specs/openid-connect-discovery-1_0.html" />
/// </summary>
public class NativeIdentityServerOpenIdConnectService : IIdentityServerOpenIdConnectService
{
    public const string AuthErrorProviderName = "provider";
    public const string BaseUrlSettingName = "Hosts:IdentityApi:BaseUrl";
    public const string JWTSigningPublicKeySettingName = "Hosts:IdentityApi:JWT:PublicKey";
    public const string ProviderName = "OpenIdConnect";
    private readonly IAuthTokensService _authTokensService;
    private readonly IEncryptionService _encryptionService;
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IOAuth2ClientService _oauth2ClientService;
    private readonly IRecorder _recorder;
    private readonly IOpenIdConnectAuthorizationRepository _repository;
    private readonly IConfigurationSettings _settings;
    private readonly ITokensService _tokensService;
    private readonly IUserProfilesService _userProfilesService;
    private readonly IWebsiteUiService _websiteUiService;

    public NativeIdentityServerOpenIdConnectService(IRecorder recorder, IIdentifierFactory identifierFactory,
        IConfigurationSettings settings, IEncryptionService encryptionService, ITokensService tokensService,
        IWebsiteUiService websiteUiService,
        IOAuth2ClientService oauth2ClientService, IAuthTokensService authTokensService,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IOpenIdConnectAuthorizationRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _settings = settings;
        _encryptionService = encryptionService;
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
        string? codeChallenge, Application.Resources.Shared.OpenIdConnectCodeChallengeMethod? codeChallengeMethod,
        CancellationToken cancellationToken)
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
                RawRedirectUri = _websiteUiService.ConstructOAuth2ConsentPageUrl(clientId, scope),
                Code = null
            };
        }

        var retrievedAuthorization =
            await _repository.FindByClientAndUserAsync(client.Id.ToId(), userId.ToId(), cancellationToken);
        if (retrievedAuthorization.IsFailure)
        {
            return retrievedAuthorization.Error;
        }

        OpenIdConnectAuthorizationRoot authorization;
        if (!retrievedAuthorization.Value.HasValue)
        {
            var created = OpenIdConnectAuthorizationRoot.Create(_recorder, _identifierFactory, _encryptionService,
                _tokensService,
                clientId.ToId(),
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
                                  ?.ToEnum<Application.Resources.Shared.OpenIdConnectCodeChallengeMethod,
                                      OpenIdConnectCodeChallengeMethod>().ToOptional()
                              ?? Optional<OpenIdConnectCodeChallengeMethod>.None;
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
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_AuthorizeCode,
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

        var authorization = retrievedAuthorization.Value.Value;
        var exchanged =
            await authorization.ExchangeCodeAsync(redirectUri, codeVerifier, OnCreateAccessTokensAsync);
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
        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUser,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.ForbiddenAccess();
        }

        if (user.Status != EndUserStatus.Registered)
        {
            return Error.ForbiddenAccess();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.NativeIdentityServerOpenIdConnectService_ExchangeCodeForToken_AccountSuspended,
                "User {Id} tried to authenticate with {Provider} with a suspended account", user.Id, ProviderName);
            return Error.EntityLocked(Resources.NativeIdentityServerOpenIdConnectService_AccountSuspended);
        }

        var maintenance = Caller.CreateAsMaintenance(caller);
        var retrievedProfile =
            await _userProfilesService.GetProfilePrivateAsync(maintenance, userId, cancellationToken);
        if (retrievedProfile.IsFailure)
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUserProfile,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        var userProfile = retrievedProfile.Value;
        _recorder.TraceInformation(caller.ToCall(), "Authorization granted for {UserId} and {ClientId}",
            userId, authorization.ClientId);
        _recorder.AuditAgainst(caller.ToCall(), userId,
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_ExchangedCode,
            "Authorization for user {Id} was granted for client {ClientId} with scopes {Scopes}, from {RedirectUri}, with nonce {Nonce}",
            userId, authorization.ClientId, authorization.Scopes.Value.Items.JoinAsOredChoices(),
            authorization.RedirectUri, authorization.Nonce);
        _recorder.TrackUsageFor(caller.ToCall(), userId,
            UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            user.ToLoginUserUsage(ProviderName, userProfile));

        var tokens = exchanged.Value;
        return tokens.ToToken(_encryptionService);

        async Task<Result<AuthTokens, Error>> OnCreateAccessTokensAsync(OpenIdConnectAuthorizationRoot auth)
        {
            var additionalData = new Dictionary<string, object>();
            if (auth.Nonce.HasValue)
            {
                additionalData.Add(AuthenticationConstants.Claims.ForNonce, auth.Nonce.Value);
            }

            additionalData.Add(AuthenticationConstants.Claims.ForClientId, auth.ClientId.ToString());

            var issued =
                await _authTokensService.IssueTokensAsync(caller, auth.UserId, auth.Scopes.Value.Items,
                    additionalData, cancellationToken);
            if (issued.IsFailure)
            {
                return issued.Error;
            }

            return issued.Value.ToAuthTokens(_encryptionService);
        }
    }

    public Task<Result<OpenIdConnectDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var baseUrl = _settings.Platform.GetString(BaseUrlSettingName);
        return Task.FromResult<Result<OpenIdConnectDiscoveryDocument, Error>>(new OpenIdConnectDiscoveryDocument
        {
            Issuer = baseUrl,
            AuthorizationEndpoint = $"{baseUrl.WithoutTrailingSlash()}{OAuth2Constants.Endpoints.Authorization}",
            TokenEndpoint = $"{baseUrl.WithoutTrailingSlash()}{OAuth2Constants.Endpoints.Token}",
            TokenEndpointAuthMethodsSupported =
            [
                OAuth2Constants.ClientAuthenticationMethods.ClientSecretBasic,
                OAuth2Constants.ClientAuthenticationMethods.ClientSecretPost
            ],
            TokenEndpointAuthSigningAlgValuesSupported = [OAuth2Constants.SigningAlgorithms.Rs256],
            UserInfoEndpoint = $"{baseUrl.WithoutTrailingSlash()}{OAuth2Constants.Endpoints.UserInfo}",
            JwksUri = $"{baseUrl.WithoutTrailingSlash()}{OpenIdConnectConstants.Endpoints.Jwks}",
            RegistrationEndPoint = "",
            ScopesSupported =
            [
                OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile,
                OAuth2Constants.Scopes.Email, OAuth2Constants.Scopes.Phone, OAuth2Constants.Scopes.Address
            ],
            ResponseTypesSupported = [OAuth2Constants.ResponseTypes.Code],
            SubjectTypesSupported = [OAuth2Constants.SubjectTypes.Public],
            UserInfoSigningAlgValuesSupported = [],
            UserInfoEncryptionAlgValuesSupported = [],
            IdTokenSigningAlgValuesSupported = [OAuth2Constants.SigningAlgorithms.Rs256],
            IdTokenEncryptionAlgValuesSupported = [],
            ClaimsSupported =
            [
                OAuth2Constants.StandardClaims.Address,
                OAuth2Constants.StandardClaims.Email,
                OAuth2Constants.StandardClaims.EmailVerified,
                OAuth2Constants.StandardClaims.FamilyName,
                OAuth2Constants.StandardClaims.GivenName,
                OAuth2Constants.StandardClaims.Locale,
                OAuth2Constants.StandardClaims.Name,
                OAuth2Constants.StandardClaims.Name,
                OAuth2Constants.StandardClaims.Nickname,
                OAuth2Constants.StandardClaims.PhoneNumber,
                OAuth2Constants.StandardClaims.PhoneNumberVerified,
                OAuth2Constants.StandardClaims.Picture,
                OAuth2Constants.StandardClaims.Zoneinfo,
                OAuth2Constants.StandardClaims.Subject
            ],
            CodeChallengeMethodsSupported =
                [OAuth2Constants.CodeChallengeMethods.Plain, OAuth2Constants.CodeChallengeMethods.S256]
        });
    }

    public Task<Result<JsonWebKeySet, Error>> GetJsonWebKeySetAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var publicKeyPem = _settings.Platform.GetString(JWTSigningPublicKeySettingName);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var parameters = rsa.ExportParameters(false);
        var keyId = Convert.ToBase64String(SHA256.HashData(parameters.Modulus!))[..8];

        return Task.FromResult<Result<JsonWebKeySet, Error>>(new JsonWebKeySet
        {
            Keys =
            [
                new JsonWebKey
                {
                    Kty = "RSA",
                    Use = "sig",
                    Kid = keyId,
                    Alg = "RS256",
                    N = Convert.ToBase64String(parameters.Modulus!),
                    E = Convert.ToBase64String(parameters.Exponent!)
                }
            ]
        });
    }

    public async Task<Result<OpenIdConnectUserInfo, Error>> GetUserInfoAsync(ICallerContext caller,
        string userId, CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData());
        }

        var token = GetAccessTokenFromCaller(caller);
        if (token.IsFailure)
        {
            return token.Error;
        }

        var accessTokenDigest = _tokensService.CreateTokenDigest(token.Value);
        var retrievedAuthorization =
            await _repository.FindByAccessTokenDigestAsync(userId.ToId(), accessTokenDigest, cancellationToken);
        if (retrievedAuthorization.IsFailure)
        {
            return retrievedAuthorization.Error;
        }

        if (!retrievedAuthorization.Value.HasValue)
        {
            return Error.ForbiddenAccess();
        }

        var authorization = retrievedAuthorization.Value.Value;
        if (!authorization.IsExchanged)
        {
            return Error.ForbiddenAccess();
        }

        var scopeList = authorization.Scopes.Value.Items.JoinAsOredChoices(string.Empty);
        var consented =
            await _oauth2ClientService.HasClientConsentedUserAsync(caller, authorization.ClientId, authorization.UserId,
                scopeList,
                cancellationToken);
        if (consented.IsFailure)
        {
            return consented.Error;
        }

        if (!consented.Value)
        {
            return Error.ForbiddenAccess();
        }

        var retrievedUser =
            await _endUsersService.GetUserPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.ForbiddenAccess();
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.ForbiddenAccess();
        }

        if (user.Status != EndUserStatus.Registered)
        {
            return Error.ForbiddenAccess();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.NativeIdentityServerOpenIdConnectService_UserInfo_AccountSuspended,
                "User {Id} tried to access user info with {Provider} with a suspended account", user.Id, ProviderName);
            return Error.EntityLocked(Resources.NativeIdentityServerOpenIdConnectService_AccountSuspended);
        }

        var profiled = await _userProfilesService.GetProfilePrivateAsync(caller, userId, cancellationToken);
        if (profiled.IsFailure)
        {
            return profiled.Error;
        }

        var profile = profiled.Value;
        var scopes = authorization.Scopes.Value.Items;
        return new OpenIdConnectUserInfo
        {
            Sub = userId,
            Name = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Profile)
                ? profile.Name.FullName()
                : null,
            Email = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Email)
                ? profile.EmailAddress
                : null,
            EmailVerified = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Email)
                ? profile.EmailAddress.HasValue()
                : null,
            GivenName = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Profile)
                ? profile.Name.FirstName
                : null,
            FamilyName = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Profile)
                ? profile.Name.LastName
                : null,
            Address = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Address)
                ? profile.Address
                : null,
            PhoneNumber = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Phone)
                ? profile.PhoneNumber
                : null,
            PhoneNumberVerified = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Phone)
                ? profile.PhoneNumber.HasValue()
                    ? false
                    : null
                : null,
            Locale = null,
            Picture = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Profile)
                ? profile.AvatarUrl
                : null,
            ZoneInfo = scopes.ContainsIgnoreCase(OAuth2Constants.Scopes.Profile)
                ? profile.Timezone
                : null
        };
    }

    public async Task<Result<OpenIdConnectTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string clientId, string clientSecret, string refreshToken, string? scope, CancellationToken cancellationToken)
    {
        var retrievedClient =
            await _oauth2ClientService.VerifyClientAsync(caller, clientId, clientSecret, cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_RefreshToken_UnknownClient,
                OAuth2Constants.ErrorCodes.InvalidClient);
        }

        var refreshTokenDigest = _tokensService.CreateTokenDigest(refreshToken);
        var client = retrievedClient.Value;
        var retrievedAuthorization =
            await _repository.FindByRefreshTokenDigestAsync(client.Id.ToId(), refreshTokenDigest, cancellationToken);
        if (retrievedAuthorization.IsFailure)
        {
            return retrievedAuthorization.Error;
        }

        if (!retrievedAuthorization.Value.HasValue)
        {
            return Error.Validation(
                Resources.NativeIdentityServerOpenIdConnectService_RefreshToken_UnknownRefreshToken,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        var authorization = retrievedAuthorization.Value.Value;
        var userId = authorization.Id;
        var retrievedUser =
            await _endUsersService.GetUserPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.ForbiddenAccess();
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.ForbiddenAccess();
        }

        if (user.Status != EndUserStatus.Registered)
        {
            return Error.ForbiddenAccess();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), userId,
                Audits.NativeIdentityServerOpenIdConnectService_RefreshToken_AccountSuspended,
                "User {Id} tried to refresh token with {Provider} with a suspended account", userId, ProviderName);
            return Error.EntityLocked(Resources.NativeIdentityServerOpenIdConnectService_AccountSuspended);
        }

        var scopes = Optional<OAuth2Scopes>.None;
        if (scope.HasValue())
        {
            var newScopes = OAuth2Scopes.Create(scope);
            if (newScopes.IsFailure)
            {
                return newScopes.Error;
            }

            scopes = newScopes.Value;
        }

        var refreshed =
            await authorization.RefreshTokenAsync(scopes, OnRefreshTokensAsync);
        if (refreshed.IsFailure)
        {
            return refreshed.Error;
        }

        var saved = await _repository.SaveAsync(authorization, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        authorization = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Authorization refreshed for {UserId} and {ClientId}",
            userId, authorization.ClientId);
        _recorder.AuditAgainst(caller.ToCall(), userId,
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_RefreshToken,
            "Authorization for user {Id} was refreshed for client {ClientId} with scopes {Scopes}, from {RedirectUri}, with nonce {Nonce}",
            userId, authorization.ClientId, authorization.Scopes.Value.Items.JoinAsOredChoices(),
            authorization.RedirectUri, authorization.Nonce);
        _recorder.TrackUsageFor(caller.ToCall(), userId,
            UsageConstants.Events.UsageScenarios.Generic.UserExtendedLogin,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.AuthProvider, ProviderName },
                { UsageConstants.Properties.UserIdOverride, userId }
            });

        var tokens = refreshed.Value;
        return tokens.ToToken(_encryptionService);

        async Task<Result<AuthTokens, Error>> OnRefreshTokensAsync(OpenIdConnectAuthorizationRoot auth)
        {
            var additionalData = new Dictionary<string, object>();
            if (auth.Nonce.HasValue)
            {
                additionalData.Add(AuthenticationConstants.Claims.ForNonce, auth.Nonce.Value);
            }

            additionalData.Add(AuthenticationConstants.Claims.ForClientId, auth.ClientId.ToString());

            var issued =
                await _authTokensService.RefreshTokensAsync(caller, refreshToken, auth.Scopes.Value.Items,
                    additionalData, cancellationToken);
            if (issued.IsFailure)
            {
                return issued.Error;
            }

            return issued.Value.ToAuthTokens(_encryptionService);
        }
    }

    private static Result<string, Error> GetAccessTokenFromCaller(ICallerContext caller)
    {
        if (!caller.IsAuthenticated)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData());
        }

        if (!caller.Authorization.HasValue)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData());
        }

        if (caller.Authorization.Value.Method != ICallerContext.AuthorizationMethod.Token
            && caller.Authorization.Value.Method != ICallerContext.AuthorizationMethod.PrivateInterHost)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData());
        }

        if (!caller.Authorization.Value.Value.HasValue)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData());
        }

        return caller.Authorization.Value.Value.Value;
    }

    private static Dictionary<string, object> GetAuthenticationErrorData()
    {
        return new Dictionary<string, object> { { AuthErrorProviderName, ProviderName } };
    }
}

public static class NativeIdentityServerOpenIdConnectServiceConversionExtensions
{
    public static Result<AuthTokens, Error> ToAuthTokens(this AuthenticateTokens issued,
        IEncryptionService encryptionService)
    {
        var accessToken = AuthToken.Create(AuthTokenType.AccessToken, issued.AccessToken.Value,
            issued.AccessToken.ExpiresOn, encryptionService);
        if (accessToken.IsFailure)
        {
            return accessToken.Error;
        }

        var refreshToken = AuthToken.Create(AuthTokenType.RefreshToken, issued.RefreshToken.Value,
            issued.RefreshToken.ExpiresOn, encryptionService);
        if (refreshToken.IsFailure)
        {
            return refreshToken.Error;
        }

        var idToken = AuthToken.Create(AuthTokenType.OtherToken, issued.IdToken!.Value,
            issued.IdToken.ExpiresOn, encryptionService);
        if (idToken.IsFailure)
        {
            return idToken.Error;
        }

        return AuthTokens.Create([accessToken.Value, refreshToken.Value, idToken.Value]);
    }

    public static OpenIdConnectTokens ToToken(this AuthTokens tokens, IEncryptionService encryptionService)
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var accessToken = tokens.GetToken(AuthTokenType.AccessToken).Value;
        var refreshToken = tokens.GetToken(AuthTokenType.RefreshToken).Value;
        var idToken = tokens.GetToken(AuthTokenType.OtherToken).Value;
        return new OpenIdConnectTokens
        {
            AccessToken = accessToken.GetDecryptedValue(encryptionService),
            TokenType = OAuth2TokenType.Bearer,
            ExpiresIn = (int)accessToken.ExpiresOn!.Value.Subtract(now).TotalSeconds,
            RefreshToken = refreshToken.GetDecryptedValue(encryptionService),
            IdToken = idToken.GetDecryptedValue(encryptionService)
        };
    }
}