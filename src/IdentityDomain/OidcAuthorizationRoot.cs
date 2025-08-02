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

[EntityName("OidcAuthorization")]
public sealed class OidcAuthorizationRoot : AggregateRootBase
{
    public delegate Task<Result<AuthTokens, Error>> CreateTokensAction(OidcAuthorizationRoot authorization);

    public static readonly TimeSpan DefaultAuthorizationCodeExpiry = TimeSpan.FromMinutes(10);
    private readonly ITokensService _tokensService;

    public static Result<OidcAuthorizationRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ITokensService tokensService, Identifier clientId, Identifier userId)
    {
        var root = new OidcAuthorizationRoot(recorder, idFactory, tokensService);
        root.RaiseCreateEvent(IdentityDomain.Events.OpenIdConnect.Authorizations.Created(root.Id, clientId, userId));
        return root;
    }

    private OidcAuthorizationRoot(IRecorder recorder, IIdentifierFactory idFactory, ITokensService tokensService) :
        base(recorder, idFactory)
    {
        _tokensService = tokensService;
    }

    private OidcAuthorizationRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties, ITokensService tokensService) : base(identifier, container,
        rehydratingProperties)
    {
        _tokensService = tokensService;
        AuthorizationCode = rehydratingProperties.GetValueOrDefault<string>(nameof(AuthorizationCode));
        AuthorizationExpiresAt = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(AuthorizationExpiresAt));
        ClientId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(ClientId));
        CodeChallenge = rehydratingProperties.GetValueOrDefault<string>(nameof(CodeChallenge));
        CodeChallengeMethod =
            rehydratingProperties.GetValueOrDefault<OAuth2CodeChallengeMethod>(nameof(CodeChallengeMethod));
        Nonce = rehydratingProperties.GetValueOrDefault<string>(nameof(Nonce));
        RedirectUri = rehydratingProperties.GetValueOrDefault<string>(nameof(RedirectUri));
        Scopes = rehydratingProperties.GetValueOrDefault<OAuth2Scopes>(nameof(Scopes));
        UserId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(UserId));
    }

    public Optional<string> AuthorizationCode { get; private set; }

    public Optional<DateTime> AuthorizationExpiresAt { get; private set; }

    public Identifier ClientId { get; private set; } = Identifier.Empty();

    public Optional<string> CodeChallenge { get; private set; }

    public Optional<OAuth2CodeChallengeMethod> CodeChallengeMethod { get; private set; }

    public bool IsAuthorized => RedirectUri.HasValue && Scopes.HasValue;

    public bool IsExchanged => AuthorizationCode.HasValue
                               && AuthorizationExpiresAt.HasValue
                               && AuthorizationExpiresAt.Value > DateTime.UtcNow;

    public bool IsRefreshable => LastExchangedAt.HasValue;

    public Optional<DateTime> LastAuthorizedAt { get; private set; }

    public Optional<DateTime> LastExchangedAt { get; private set; }

    public Optional<string> Nonce { get; private set; }

    public Optional<string> RedirectUri { get; private set; }

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
    public static AggregateRootFactory<OidcAuthorizationRoot> Rehydrate()
    {
        return (identifier, container, properties) => new OidcAuthorizationRoot(identifier, container, properties,
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
            return Error.RuleViolation(Resources.OidcAuthorizationRoot_MissingUserId);
        }

        if (ClientId.IsEmpty())
        {
            return Error.RuleViolation(Resources.OidcAuthorizationRoot_MissingClientId);
        }

        if (IsAuthorized)
        {
            if (!RedirectUri.HasValue)
            {
                return Error.RuleViolation(Resources.OidcAuthorizationRoot_MissingRedirectUri);
            }

            if (!Scopes.HasValue || Scopes.Value.HasNone)
            {
                return Error.RuleViolation(Resources.OidcAuthorizationRoot_MissingScopes);
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
                AuthorizationCode = authorized.Code;
                AuthorizationExpiresAt = authorized.ExpiresAt;
                CodeChallenge = authorized.CodeChallenge;
                CodeChallengeMethod = authorized.CodeChallengeMethod.ToOptional();
                LastAuthorizedAt = authorized.AuthorizedAt;
                Nonce = authorized.Nonce;
                RedirectUri = authorized.RedirectUri;
                var scopes = OAuth2Scopes.Create(authorized.Scopes);
                if (scopes.IsFailure)
                {
                    return scopes.Error;
                }

                Scopes = scopes.Value;
                Recorder.TraceDebug(null,
                    "OidcAuthorization {Id} permission has been authorized for {RedirectUri} and scopes {Scopes}", Id,
                    RedirectUri,
                    Scopes);
                return Result.Ok;
            }

            case CodeExchanged exchanged:
            {
                AuthorizationExpiresAt = Optional<DateTime>.None;
                AuthorizationCode = Optional<string>.None;
                LastExchangedAt = exchanged.ExchangedAt;
                Recorder.TraceDebug(null,
                    "OidcAuthorization {Id} has been exchanged for {RedirectUri} and scopes {Scopes}", Id, RedirectUri,
                    Scopes);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AuthorizeCode(Optional<string> clientRedirectUri, string redirectUri,
        OAuth2Scopes scopes, Optional<string> nonce, Optional<string> codeChallenge,
        Optional<OAuth2CodeChallengeMethod> codeChallengeMethod)
    {
        if (!clientRedirectUri.HasValue)
        {
            return Error.PreconditionViolation(Resources.OidcAuthorizationRoot_GenerateCode_MissingClientRedirectUri);
        }

        if (IsMismatchedRedirectUri(clientRedirectUri.Value, redirectUri))
        {
            return Error.PreconditionViolation(Resources
                .OidcAuthorizationRoot_GenerateCode_MismatchedClientRedirectUri);
        }

        if (!scopes.Has(OpenIdConnectConstants.Scopes.OpenId))
        {
            return Error.PreconditionViolation(Resources.OidcAuthorizationRoot_GenerateCode_MissingOpenIdScope);
        }

        if (codeChallenge.HasValue && !codeChallengeMethod.HasValue)
        {
            return Error.PreconditionViolation(
                Resources.OidcAuthorizationRoot_GenerateCode_CodeChallengeMissingMethod);
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
            IdentityDomain.Events.OpenIdConnect.Authorizations.CodeAuthorized(Id, ClientId, UserId, scopes, redirectUri,
                nonce, codeChallenge, codeChallengeMethod, code, expiresAt));
    }

    public async Task<Result<AuthTokens, Error>> ExchangeCodeAsync(string redirectUri, string? codeVerifier,
        CreateTokensAction createTokens)
    {
        if (!IsAuthorized)
        {
            return Error.PreconditionViolation(Resources.OidcAuthorizationRoot_VerifyCode_NotConfigured,
                OAuth2Constants.ErrorCodes.InvalidClient);
        }

        if (!IsExchanged)
        {
            return Error.Validation(Resources.OidcAuthorizationRoot_VerifyCode_ExpiredAuthorizationCode,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        if (IsMismatchedRedirectUri(redirectUri))
        {
            return Error.Validation(Resources.OidcAuthorizationRoot_VerifyCode_MismatchedRedirectUri,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        var pkceVerification = VerifyPkce(codeVerifier, CodeChallenge, CodeChallengeMethod);
        if (pkceVerification.IsFailure)
        {
            return pkceVerification.Error;
        }

        var tokens = await createTokens(this);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        var exchanged = RaiseChangeEvent(
            IdentityDomain.Events.OpenIdConnect.Authorizations.CodeExchanged(Id));
        if (exchanged.IsFailure)
        {
            return exchanged.Error;
        }

        return tokens.Value;
    }

    public Result<Error> RefreshToken()
    {
        return RaiseChangeEvent(
            IdentityDomain.Events.OpenIdConnect.Authorizations.TokenRefreshed(Id));
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
        Optional<OAuth2CodeChallengeMethod> codeChallengeMethod)
    {
        if (codeVerifier.HasValue() && !codeChallenge.HasValue)
        {
            return Error.Validation(Resources.OidcAuthorizationRoot_VerifyPkce_MissingCodeChallenge,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        if (!codeChallenge.HasValue)
        {
            return Result.Ok;
        }

        if (codeVerifier.HasNoValue())
        {
            return Error.Validation(Resources.OidcAuthorizationRoot_VerifyPkce_MissingCodeVerifier,
                OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        if (IsFailedChallenge(codeChallenge.Value, codeChallengeMethod.Value, codeVerifier))
        {
            return Error.Validation(Resources.OidcAuthorizationRoot_VerifyPkce_InvalidCodeVerifier,
                OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        return Result.Ok;

        static bool IsFailedChallenge(string codeChallenge, OAuth2CodeChallengeMethod codeChallengeMethod,
            string codeVerifier)
        {
            switch (codeChallengeMethod)
            {
                case OAuth2CodeChallengeMethod.Plain:
                    return codeChallenge.NotEqualsOrdinal(codeVerifier);

                case OAuth2CodeChallengeMethod.S256:
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
        var code = _tokensService.CreateOAuthAuthorizationCode(combined);

        return $"{ClientId}:{code}";
    }
}