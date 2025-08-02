using System.Security.Cryptography;
using System.Text;
using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Identities;
using JetBrains.Annotations;
using QueryAny;

namespace IdentityDomain;

[EntityName("OpenIdConnectAuthorization")]
public sealed class OpenIdConnectAuthorizationRoot : AggregateRootBase
{
    public delegate Task<Result<AuthTokens, Error>> CreateTokensAction(OpenIdConnectAuthorizationRoot authorization);

    public static readonly TimeSpan DefaultAuthorizationCodeExpiry = TimeSpan.FromMinutes(10);
    private readonly IEncryptionService _encryptionService;
    private readonly ITokensService _tokensService;

    public static Result<OpenIdConnectAuthorizationRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IEncryptionService encryptionService, ITokensService tokensService, Identifier clientId, Identifier userId)
    {
        var root = new OpenIdConnectAuthorizationRoot(recorder, idFactory, encryptionService, tokensService);
        root.RaiseCreateEvent(IdentityDomain.Events.OpenIdConnect.Created(root.Id, clientId, userId));
        return root;
    }

    private OpenIdConnectAuthorizationRoot(IRecorder recorder, IIdentifierFactory idFactory,
        IEncryptionService encryptionService, ITokensService tokensService) :
        base(recorder, idFactory)
    {
        _encryptionService = encryptionService;
        _tokensService = tokensService;
    }

    private OpenIdConnectAuthorizationRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties, IEncryptionService encryptionService,
        ITokensService tokensService) : base(identifier, container,
        rehydratingProperties)
    {
        _encryptionService = encryptionService;
        _tokensService = tokensService;
        AuthorizationCode = rehydratingProperties.GetValueOrDefault<string>(nameof(AuthorizationCode));
        AuthorizationExpiresAt = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(AuthorizationExpiresAt));
        ClientId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(ClientId));
        CodeChallenge = rehydratingProperties.GetValueOrDefault<string>(nameof(CodeChallenge));
        CodeChallengeMethod =
            rehydratingProperties.GetValueOrDefault<OpenIdConnectCodeChallengeMethod>(nameof(CodeChallengeMethod));
        Nonce = rehydratingProperties.GetValueOrDefault<string>(nameof(Nonce));
        RedirectUri = rehydratingProperties.GetValueOrDefault<string>(nameof(RedirectUri));
        Scopes = rehydratingProperties.GetValueOrDefault<OAuth2Scopes>(nameof(Scopes));
        UserId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(UserId));
    }

    public Optional<OAuth2TokenMemento> AccessToken { get; private set; }

    public Optional<string> AuthorizationCode { get; private set; }

    public Optional<DateTime> AuthorizationExpiresAt { get; private set; }

    public Optional<DateTime> AuthorizedAt { get; private set; }

    public Identifier ClientId { get; private set; } = Identifier.Empty();

    public Optional<string> CodeChallenge { get; private set; }

    public Optional<OpenIdConnectCodeChallengeMethod> CodeChallengeMethod { get; private set; }

    public Optional<DateTime> CodeExchangedAt { get; private set; }

    public bool IsAuthorized => AuthorizedAt.HasValue
                                && AuthorizationCode.HasValue
                                && AuthorizationExpiresAt.HasValue
                                && RedirectUri.HasValue
                                && Scopes.HasValue;

    public bool IsExchangable => IsAuthorized
                                 && AuthorizationExpiresAt.Value > DateTime.UtcNow;

    public bool IsExchanged => AuthorizedAt.HasValue
                               && !AuthorizationCode.HasValue
                               && !AuthorizationExpiresAt.HasValue
                               && CodeExchangedAt.HasValue
                               && RefreshToken.HasValue;

    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsRefreshable => IsExchanged
                                 && RefreshToken.Value.ExpiresOn > DateTime.UtcNow;

    // ReSharper disable once UnusedMember.Global
    public bool IsRefreshed => IsExchanged
                               && LastRefreshedAt.HasValue
                               && RefreshToken.HasValue;

    public Optional<DateTime> LastRefreshedAt { get; private set; }

    public Optional<string> Nonce { get; private set; }

    public Optional<string> RedirectUri { get; private set; }

    public Optional<OAuth2TokenMemento> RefreshToken { get; private set; }

    public Optional<OAuth2Scopes> Scopes { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public override HydrationProperties Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(AuthorizationCode), AuthorizationCode);
        properties.Add(nameof(AuthorizationExpiresAt), AuthorizationExpiresAt);
        properties.Add(nameof(ClientId), ClientId);
        properties.Add(nameof(CodeChallenge), CodeChallenge);
        properties.Add(nameof(CodeChallengeMethod), CodeChallengeMethod);
        properties.Add(nameof(Nonce), Nonce);
        properties.Add(nameof(RedirectUri), RedirectUri);
        properties.Add(nameof(Scopes), Scopes);
        properties.Add(nameof(UserId), UserId);
        return properties;
    }

    [UsedImplicitly]
    public static AggregateRootFactory<OpenIdConnectAuthorizationRoot> Rehydrate()
    {
        return (identifier, container, properties) => new OpenIdConnectAuthorizationRoot(identifier, container,
            properties, container.GetRequiredService<IEncryptionService>(),
            container.GetRequiredService<ITokensService>());
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        if (UserId.IsEmpty())
        {
            return Error.RuleViolation(Resources.OpenIdConnectAuthorizationRootMissingUserId);
        }

        if (ClientId.IsEmpty())
        {
            return Error.RuleViolation(Resources.OpenIdConnectAuthorizationRootMissingClientId);
        }

        if (IsAuthorized)
        {
            if (!RedirectUri.HasValue)
            {
                return Error.RuleViolation(Resources.OpenIdConnectAuthorizationRootMissingRedirectUri);
            }

            if (!Scopes.HasValue || Scopes.Value.HasNone)
            {
                return Error.RuleViolation(Resources.OpenIdConnectAuthorizationRootMissingScopes);
            }
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                ClientId = created.ClientId.ToId();
                UserId = created.UserId.ToId();
                return Result.Ok;
            }

            case CodeAuthorized authorized:
            {
                AuthorizedAt = authorized.AuthorizedAt;
                AuthorizationCode = authorized.Code;
                AuthorizationExpiresAt = authorized.ExpiresAt;
                CodeChallenge = authorized.CodeChallenge;
                CodeChallengeMethod = authorized.CodeChallengeMethod.ToOptional();
                Nonce = authorized.Nonce;
                RedirectUri = authorized.RedirectUri;
                var scopes = OAuth2Scopes.Create(authorized.Scopes);
                if (scopes.IsFailure)
                {
                    return scopes.Error;
                }

                Scopes = scopes.Value;
                Recorder.TraceDebug(null,
                    "OIDC Authorization {Id} permission has been authorized for {RedirectUri} and scopes {Scopes}", Id,
                    RedirectUri,
                    Scopes);
                return Result.Ok;
            }

            case CodeExchanged exchanged:
            {
                AuthorizationCode = Optional<string>.None;
                AuthorizationExpiresAt = Optional<DateTime>.None;
                CodeExchangedAt = exchanged.ExchangedAt;
                var accessToken = OAuth2TokenMemento.Create(AuthTokenType.AccessToken, exchanged.AccessTokenDigest,
                    exchanged.AccessTokenExpiresOn);
                if (accessToken.IsFailure)
                {
                    return accessToken.Error;
                }

                AccessToken = accessToken.Value;
                var refreshToken = OAuth2TokenMemento.Create(AuthTokenType.RefreshToken, exchanged.RefreshTokenDigest,
                    exchanged.RefreshTokenExpiresOn);
                if (refreshToken.IsFailure)
                {
                    return refreshToken.Error;
                }

                RefreshToken = refreshToken.Value;
                Recorder.TraceDebug(null,
                    "OIDC Authorization {Id} has been exchanged for {RedirectUri} and scopes {Scopes}", Id, RedirectUri,
                    Scopes);
                return Result.Ok;
            }

            case TokenRefreshed refreshed:
            {
                LastRefreshedAt = refreshed.RefreshedAt;
                var accessToken = OAuth2TokenMemento.Create(AuthTokenType.AccessToken, refreshed.AccessTokenDigest,
                    refreshed.AccessTokenExpiresOn);
                if (accessToken.IsFailure)
                {
                    return accessToken.Error;
                }

                AccessToken = accessToken.Value;
                var refreshToken = OAuth2TokenMemento.Create(AuthTokenType.RefreshToken, refreshed.RefreshTokenDigest,
                    refreshed.RefreshTokenExpiresOn);
                if (refreshToken.IsFailure)
                {
                    return refreshToken.Error;
                }

                RefreshToken = refreshToken.Value;
                var scopes = OAuth2Scopes.Create(refreshed.Scopes);
                if (scopes.IsFailure)
                {
                    return scopes.Error;
                }

                Scopes = scopes.Value;
                Recorder.TraceDebug(null,
                    "OIDC Authorization {Id} has been refreshed for {RedirectUri} and scopes {Scopes}", Id, RedirectUri,
                    Scopes);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AuthorizeCode(Optional<string> clientRedirectUri, string redirectUri,
        OAuth2Scopes scopes, Optional<string> nonce, Optional<string> codeChallenge,
        Optional<OpenIdConnectCodeChallengeMethod> codeChallengeMethod)
    {
        if (!clientRedirectUri.HasValue)
        {
            return Error.PreconditionViolation(Resources
                .OpenIdConnectAuthorizationRootGenerateCode_MissingClientRedirectUri);
        }

        if (IsMismatchedRedirectUri(clientRedirectUri.Value, redirectUri))
        {
            return Error.PreconditionViolation(Resources
                .OpenIdConnectAuthorizationRootGenerateCode_MismatchedClientRedirectUri);
        }

        if (!scopes.Has(OpenIdConnectConstants.Scopes.OpenId))
        {
            return Error.PreconditionViolation(Resources.OpenIdConnectAuthorizationRootMissingOpenIdScope);
        }

        if (codeChallenge.HasValue && !codeChallengeMethod.HasValue)
        {
            return Error.PreconditionViolation(
                Resources.OpenIdConnectAuthorizationRootGenerateCode_CodeChallengeMissingMethod);
        }

        var nothingHasChanged = RedirectUri == redirectUri
                                && Scopes == scopes
                                && Nonce == nonce
                                && CodeChallenge == codeChallenge
                                && CodeChallengeMethod == codeChallengeMethod;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        var code = GenerateAuthorizationCode(redirectUri, scopes, nonce, codeChallenge);
        var expiresAt = DateTime.UtcNow.Add(DefaultAuthorizationCodeExpiry);

        return RaiseChangeEvent(
            IdentityDomain.Events.OpenIdConnect.CodeAuthorized(Id, ClientId, UserId, scopes, redirectUri,
                nonce, codeChallenge, codeChallengeMethod, code, expiresAt));
    }

    public async Task<Result<AuthTokens, Error>> ExchangeCodeAsync(string redirectUri, string? codeVerifier,
        CreateTokensAction onCreateTokens)
    {
        if (!IsAuthorized)
        {
            return Error.PreconditionViolation(Resources.OpenIdConnectAuthorizationRootVerifyCode_NotConfigured,
                OAuth2Constants.ErrorCodes.InvalidClient);
        }

        if (!IsExchangable)
        {
            return Error.PreconditionViolation(
                Resources.OpenIdConnectAuthorizationRootVerifyCode_ExpiredAuthorizationCode,
                OAuth2Constants.ErrorCodes.AccessDenied);
        }

        if (IsMismatchedRedirectUri(redirectUri))
        {
            return Error.Validation(Resources.OpenIdConnectAuthorizationRootVerifyCode_MismatchedRedirectUri,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        var pkceVerification = VerifyPkce(codeVerifier, CodeChallenge, CodeChallengeMethod);
        if (pkceVerification.IsFailure)
        {
            return pkceVerification.Error;
        }

        var tokens = await onCreateTokens(this);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        var accessToken = tokens.Value.GetToken(AuthTokenType.AccessToken);
        var accessTokenMemento = OAuth2TokenMemento.Create(accessToken, _encryptionService, _tokensService);
        if (accessTokenMemento.IsFailure)
        {
            return accessTokenMemento.Error;
        }

        var refreshToken = tokens.Value.GetToken(AuthTokenType.RefreshToken);
        var refreshTokenMemento = OAuth2TokenMemento.Create(refreshToken, _encryptionService, _tokensService);
        if (refreshTokenMemento.IsFailure)
        {
            return refreshTokenMemento.Error;
        }

        var exchanged = RaiseChangeEvent(
            IdentityDomain.Events.OpenIdConnect.CodeExchanged(Id, accessTokenMemento.Value, refreshTokenMemento.Value));
        if (exchanged.IsFailure)
        {
            return exchanged.Error;
        }

        return tokens.Value;
    }

    public async Task<Result<AuthTokens, Error>> RefreshTokenAsync(Optional<OAuth2Scopes> scopes,
        CreateTokensAction onRefreshTokens)
    {
        if (!IsRefreshable)
        {
            return Error.PreconditionViolation(Resources.OpenIdConnectAuthorizationRootRefreshToken_RefreshForbidden,
                OAuth2Constants.ErrorCodes.AccessDenied);
        }

        var refreshedScopes = Scopes.Value;
        if (scopes.HasValue)
        {
            if (!Scopes.HasValue
                || !scopes.Value.IsSubsetOf(Scopes.Value))
            {
                return Error.PreconditionViolation(Resources.OpenIdConnectAuthorizationRootRefreshToken_ScopesNotSubset,
                    OAuth2Constants.ErrorCodes.InvalidScope);
            }

            if (!scopes.Value.Has(OpenIdConnectConstants.Scopes.OpenId))
            {
                return Error.PreconditionViolation(Resources.OpenIdConnectAuthorizationRootMissingOpenIdScope);
            }

            refreshedScopes = scopes.Value;
        }

        var tokens = await onRefreshTokens(this);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        var accessToken = tokens.Value.GetToken(AuthTokenType.AccessToken);

        var accessTokenMemento = OAuth2TokenMemento.Create(accessToken, _encryptionService, _tokensService);
        if (accessTokenMemento.IsFailure)
        {
            return accessTokenMemento.Error;
        }

        var refreshToken = tokens.Value.GetToken(AuthTokenType.AccessToken);
        var refreshTokenMemento = OAuth2TokenMemento.Create(refreshToken, _encryptionService, _tokensService);
        if (refreshTokenMemento.IsFailure)
        {
            return refreshTokenMemento.Error;
        }

        var exchanged = RaiseChangeEvent(
            IdentityDomain.Events.OpenIdConnect.TokenRefreshed(Id, accessTokenMemento.Value, refreshTokenMemento.Value,
                refreshedScopes));
        if (exchanged.IsFailure)
        {
            return exchanged.Error;
        }

        return tokens.Value;
    }

#if TESTINGONLY
    public void TestingOnly_ExpireAuthorizationCode()
    {
        AuthorizationExpiresAt = DateTime.UtcNow.SubtractSeconds(1);
    }
#endif

    private bool IsMismatchedRedirectUri(string redirectUri)
    {
        return IsMismatchedRedirectUri(redirectUri, RedirectUri);
    }

    private static bool IsMismatchedRedirectUri(string redirectUri, string otherRedirectUri)
    {
        return redirectUri.NotEqualsIgnoreCase(otherRedirectUri);
    }

    private static Result<Error> VerifyPkce(string? codeVerifier, Optional<string> codeChallenge,
        Optional<OpenIdConnectCodeChallengeMethod> codeChallengeMethod)
    {
        if (codeVerifier.HasValue() && !codeChallenge.HasValue)
        {
            return Error.Validation(Resources.OpenIdConnectAuthorizationRootVerifyPkce_MissingCodeChallenge,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        if (!codeChallenge.HasValue)
        {
            return Result.Ok;
        }

        if (codeVerifier.HasNoValue())
        {
            return Error.Validation(Resources.OpenIdConnectAuthorizationRootVerifyPkce_MissingCodeVerifier,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        if (IsFailedChallenge(codeChallenge.Value, codeChallengeMethod.Value, codeVerifier))
        {
            return Error.Validation(Resources.OpenIdConnectAuthorizationRootVerifyPkce_InvalidCodeVerifier,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        return Result.Ok;

        static bool IsFailedChallenge(string codeChallenge, OpenIdConnectCodeChallengeMethod codeChallengeMethod,
            string codeVerifier)
        {
            switch (codeChallengeMethod)
            {
                case OpenIdConnectCodeChallengeMethod.Plain:
                    return codeChallenge.NotEqualsOrdinal(codeVerifier);

                case OpenIdConnectCodeChallengeMethod.S256:
                    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
                    var base64Hash = Convert.ToBase64String(hash);
                    return codeChallenge.NotEqualsOrdinal(base64Hash);

                default:
                    return true;
            }
        }
    }

    private string GenerateAuthorizationCode(string redirectUri, OAuth2Scopes scopes, Optional<string> nonce,
        Optional<string> codeChallenge)
    {
        var scope = scopes.Items.JoinAsOredChoices();
        var combined = new string[] { ClientId, UserId, redirectUri, scope, nonce, codeChallenge }
            .Join("|");
        var code = _tokensService.CreateOAuthorizationCodeDigest(combined);

        return $"{ClientId}:{code}";
    }
}