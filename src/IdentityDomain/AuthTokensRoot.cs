using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using JetBrains.Annotations;
using QueryAny;

namespace IdentityDomain;

/// <summary>
///     Manages the authentication tokens, that the Identity subdomain creates to access the entire product.
///     Note: We've decided to make this aggregate snapshotting because over the lifetime of a user account (let's say 15
///     years), it is possible to have renewed this token every 15 minutes in that time frame, which would generate
///     thousands and thousands of events in a user's lifetime, which would make loading this aggregate into memory
///     progressively slower over time.
///     At present, we are not actually too interested in the history of the token value, just the current state of it.
///     We need to store the token values encrypted at rest, and we also need to store
///     a digest of the <see cref="RefreshTokenDigest" /> in plain text for matching on, for renewals.
/// </summary>
[EntityName("AuthToken")]
public sealed class AuthTokensRoot : AggregateRootBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly ITokensService _tokensService;

    public static Result<AuthTokensRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IEncryptionService encryptionService, ITokensService tokensService, Identifier userId)
    {
        var root = new AuthTokensRoot(recorder, idFactory, encryptionService, tokensService);
        root.RaiseCreateEvent(IdentityDomain.Events.AuthTokens.Created(root.Id, userId));
        return root;
    }

    private AuthTokensRoot(IRecorder recorder, IIdentifierFactory idFactory, IEncryptionService encryptionService,
        ITokensService tokensService) :
        base(recorder, idFactory)
    {
        _encryptionService = encryptionService;
        _tokensService = tokensService;
    }

    private AuthTokensRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
        _encryptionService = container.GetRequiredService<IEncryptionService>();
        _tokensService = container.GetRequiredService<ITokensService>();

        AccessToken = rehydratingProperties.GetValueOrDefault<string>(nameof(AccessToken));
        AccessTokenExpiresOn = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(AccessTokenExpiresOn));
        RefreshToken = rehydratingProperties.GetValueOrDefault<string>(nameof(RefreshToken));
        RefreshTokenDigest = rehydratingProperties.GetValueOrDefault<string>(nameof(RefreshTokenDigest));
        RefreshTokenExpiresOn = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(RefreshTokenExpiresOn));
        IdToken = rehydratingProperties.GetValueOrDefault<string>(nameof(IdToken));
        IdTokenExpiresOn = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(IdTokenExpiresOn));
        UserId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(UserId));
    }

    public Optional<string> AccessToken { get; private set; }

    public Optional<DateTime> AccessTokenExpiresOn { get; private set; }

    public Optional<string> IdToken { get; private set; }

    public Optional<DateTime> IdTokenExpiresOn { get; private set; }

    public bool IsRefreshTokenExpired => !IsRevoked && DateTime.UtcNow > RefreshTokenExpiresOn.Value;

    public bool IsRevoked => !AccessToken.HasValue
                             && !RefreshToken.HasValue
                             && !IdToken.HasValue
                             && !AccessTokenExpiresOn.HasValue
                             && !RefreshTokenExpiresOn.HasValue
                             && !IdTokenExpiresOn.HasValue;

    public Optional<string> RefreshToken { get; private set; }

    public Optional<string> RefreshTokenDigest { get; private set; }

    public Optional<DateTime> RefreshTokenExpiresOn { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public override HydrationProperties Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(AccessToken), AccessToken);
        properties.Add(nameof(AccessTokenExpiresOn), AccessTokenExpiresOn);
        properties.Add(nameof(RefreshToken), RefreshToken);
        properties.Add(nameof(RefreshTokenDigest), RefreshTokenDigest);
        properties.Add(nameof(RefreshTokenExpiresOn), RefreshTokenExpiresOn);
        properties.Add(nameof(IdToken), IdToken);
        properties.Add(nameof(IdTokenExpiresOn), IdTokenExpiresOn);
        properties.Add(nameof(UserId), UserId);
        return properties;
    }

    [UsedImplicitly]
    public static AggregateRootFactory<AuthTokensRoot> Rehydrate()
    {
        return (identifier, container, properties) => new AuthTokensRoot(identifier, container, properties);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                UserId = created.UserId.ToId();
                return Result.Ok;
            }

            case TokensChanged changed:
            {
                AccessToken = changed.AccessToken;
                RefreshToken = changed.RefreshToken;
                IdToken = changed.IdToken;
                AccessTokenExpiresOn = changed.AccessTokenExpiresOn.ToOptional();
                RefreshTokenExpiresOn = changed.RefreshTokenExpiresOn.ToOptional();
                IdTokenExpiresOn = changed.IdTokenExpiresOn.ToOptional();
                RefreshTokenDigest = changed.RefreshTokenDigest;
                Recorder.TraceDebug(null, "AuthTokens {Id} were changed for {UserId}", Id, changed.UserId);
                return Result.Ok;
            }

            case TokensRefreshed changed:
            {
                AccessToken = changed.AccessToken;
                RefreshToken = changed.RefreshToken;
                IdToken = changed.IdToken;
                AccessTokenExpiresOn = changed.AccessTokenExpiresOn.ToOptional();
                RefreshTokenExpiresOn = changed.RefreshTokenExpiresOn.ToOptional();
                IdTokenExpiresOn = changed.IdTokenExpiresOn.ToOptional();
                RefreshTokenDigest = changed.RefreshTokenDigest;
                Recorder.TraceDebug(null, "AuthTokens {Id} were refreshed for {UserId}", Id, changed.UserId);
                return Result.Ok;
            }

            case TokensRevoked changed:
            {
                AccessToken = Optional<string>.None;
                RefreshToken = Optional<string>.None;
                IdToken = Optional<string>.None;
                AccessTokenExpiresOn = Optional<DateTime>.None;
                RefreshTokenExpiresOn = Optional<DateTime>.None;
                IdTokenExpiresOn = Optional<DateTime>.None;
                RefreshTokenDigest = Optional<string>.None;
                Recorder.TraceDebug(null, "AuthTokens {Id} were revoked for {UserId}", Id, changed.UserId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> RenewTokens(string refreshTokenToRenew, AuthToken accessToken, AuthToken refreshToken,
        Optional<AuthToken> idToken)
    {
        if (IsRevoked)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_TokensRevoked);
        }

        var decrypted = _encryptionService.Decrypt(RefreshToken.Value);
        if (!decrypted.Equals(refreshTokenToRenew))
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_RefreshTokenNotMatched);
        }

        if (IsRefreshTokenExpired)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_RefreshTokenExpired);
        }

        var refreshTokenDigest = _tokensService.CreateTokenDigest(refreshTokenToRenew);

        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensRefreshed(Id, UserId, accessToken, refreshToken, idToken,
                refreshTokenDigest));
    }

    public Result<Error> Revoke(string refreshToken)
    {
        if (IsRevoked)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_TokensRevoked);
        }

        var decrypted = _encryptionService.Decrypt(RefreshToken.Value);
        if (!decrypted.Equals(refreshToken))
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_RefreshTokenNotMatched);
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensRevoked(Id, UserId));
    }

    public Result<Error> SetTokens(AuthToken accessToken, AuthToken refreshToken, Optional<AuthToken> idToken)
    {
        var threshold = DateTime.UtcNow.AddSeconds(5);
        if (accessToken.ExpiresOn < threshold)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_TokensExpired);
        }

        var refreshTokenDecrypted = refreshToken.GetDecryptedValue(_encryptionService);
        var refreshTokenDigest = _tokensService.CreateTokenDigest(refreshTokenDecrypted);

        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensChanged(Id, UserId, accessToken, refreshToken, idToken,
                refreshTokenDigest));
    }

#if TESTINGONLY
    public void TestingOnly_SetTokens(string accessToken, string refreshToken, string? idToken,
        DateTime accessTokenExpiresOn, DateTime refreshTokenExpiresOn, DateTime? idTokenExpiresOn)
    {
        var refreshTokenDigest = _tokensService.CreateTokenDigest(refreshToken);
        RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensChanged(Id, UserId,
                AuthToken.Create(AuthTokenType.AccessToken, accessToken, accessTokenExpiresOn).Value,
                AuthToken.Create(AuthTokenType.RefreshToken, refreshToken, refreshTokenExpiresOn).Value,
                idToken.HasValue()
                    ? AuthToken.Create(AuthTokenType.OtherToken, idToken, idTokenExpiresOn).Value
                    : Optional<AuthToken>.None, refreshTokenDigest));
    }
#endif
}