using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class AuthTokensRoot : AggregateRootBase
{
    public static Result<AuthTokensRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier userId)
    {
        var root = new AuthTokensRoot(recorder, idFactory);
        root.RaiseCreateEvent(IdentityDomain.Events.AuthTokens.Created(root.Id, userId));
        return root;
    }

    private AuthTokensRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AuthTokensRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Optional<string> AccessToken { get; private set; }

    public Optional<DateTime> AccessTokenExpiresOn { get; private set; }

    public bool IsRefreshTokenExpired => !IsRevoked && DateTime.UtcNow > RefreshTokenExpiresOn.Value;

    public bool IsRevoked => !RefreshToken.HasValue && !AccessToken.HasValue && !AccessTokenExpiresOn.HasValue
                             && !RefreshTokenExpiresOn.HasValue;

    public Optional<string> RefreshToken { get; private set; }

    public Optional<DateTime> RefreshTokenExpiresOn { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    [UsedImplicitly]
    public static AggregateRootFactory<AuthTokensRoot> Rehydrate()
    {
        return (identifier, container, _) => new AuthTokensRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
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
                AccessTokenExpiresOn = changed.AccessTokenExpiresOn;
                RefreshTokenExpiresOn = changed.RefreshTokenExpiresOn;
                Recorder.TraceDebug(null, "AuthTokens {Id} were changed for {UserId}", Id, changed.UserId);
                return Result.Ok;
            }

            case TokensRefreshed changed:
            {
                AccessToken = changed.AccessToken;
                RefreshToken = changed.RefreshToken;
                AccessTokenExpiresOn = changed.AccessTokenExpiresOn;
                RefreshTokenExpiresOn = changed.RefreshTokenExpiresOn;
                Recorder.TraceDebug(null, "AuthTokens {Id} were refreshed for {UserId}", Id, changed.UserId);
                return Result.Ok;
            }

            case TokensRevoked changed:
            {
                AccessToken = Optional<string>.None;
                RefreshToken = Optional<string>.None;
                AccessTokenExpiresOn = Optional<DateTime>.None;
                RefreshTokenExpiresOn = Optional<DateTime>.None;
                Recorder.TraceDebug(null, "AuthTokens {Id} were revoked for {UserId}", Id, changed.UserId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> RenewTokens(string refreshTokenToRenew, string accessToken, string refreshToken,
        DateTime accessTokenExpiresOn, DateTime refreshTokenExpiresOn)
    {
        if (IsRevoked)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_TokensRevoked);
        }

        if (RefreshToken != refreshTokenToRenew)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_RefreshTokenNotMatched);
        }

        if (IsRefreshTokenExpired)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_RefreshTokenExpired);
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensRefreshed(Id, UserId, accessToken, accessTokenExpiresOn,
                refreshToken, refreshTokenExpiresOn));
    }

    public Result<Error> Revoke(string refreshToken)
    {
        if (IsRevoked)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_TokensRevoked);
        }

        if (RefreshToken != refreshToken)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_RefreshTokenNotMatched);
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensRevoked(Id, UserId));
    }

    public Result<Error> SetTokens(string accessToken, string refreshToken, DateTime accessTokenExpiresOn,
        DateTime refreshTokenExpiresOn)
    {
        var threshold = DateTime.UtcNow.AddSeconds(5);
        if (accessTokenExpiresOn < threshold)
        {
            return Error.RuleViolation(Resources.AuthTokensRoot_TokensExpired);
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensChanged(Id, UserId, accessToken, accessTokenExpiresOn,
                refreshToken, refreshTokenExpiresOn));
    }

#if TESTINGONLY
    public Result<Error> TestingOnly_SetTokens(string accessToken, string refreshToken, DateTime accessTokenExpiresOn,
        DateTime refreshTokenExpiresOn)
    {
        return RaiseChangeEvent(
            IdentityDomain.Events.AuthTokens.TokensChanged(Id, UserId, accessToken, accessTokenExpiresOn,
                refreshToken, refreshTokenExpiresOn));
    }
#endif
}