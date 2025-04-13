using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;

namespace IdentityApplication;

public class AuthTokensApplication : IAuthTokensApplication
{
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _idFactory;
    private readonly IJWTTokensService _jwtTokensService;
    private readonly IRecorder _recorder;
    private readonly IAuthTokensRepository _repository;

    public AuthTokensApplication(IRecorder recorder, IIdentifierFactory idFactory, IJWTTokensService jwtTokensService,
        IEndUsersService endUsersService, IAuthTokensRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _jwtTokensService = jwtTokensService;
        _endUsersService = endUsersService;
        _repository = repository;
    }

    public async Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext caller, EndUserWithMemberships user,
        CancellationToken cancellationToken)
    {
        var issued = await _jwtTokensService.IssueTokensAsync(user);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        var retrieved = await _repository.FindByUserIdAsync(user.Id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        AuthTokensRoot authTokens;
        if (retrieved.Value.HasValue)
        {
            authTokens = retrieved.Value.Value;
        }
        else
        {
            var root = AuthTokensRoot.Create(_recorder, _idFactory, user.Id.ToId());
            if (root.IsFailure)
            {
                return root.Error;
            }

            authTokens = root.Value;
        }

        var set = authTokens.SetTokens(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresOn,
            tokens.RefreshTokenExpiresOn);
        if (set.IsFailure)
        {
            return set.Error;
        }

        var saved = await _repository.SaveAsync(authTokens, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        authTokens = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "AuthTokens were issued for {Id}", authTokens.Id);

        return tokens;
    }

    public async Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByRefreshTokenAsync(refreshToken, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var authTokens = retrieved.Value.Value;
        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, authTokens.UserId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.NotAuthenticated();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.AuthTokensApplication_Refresh_AccountSuspended,
                "User {Id} tried to refresh AuthTokens with a suspended account", user.Id);
            return Error.EntityLocked(Resources.AuthTokensApplication_AccountSuspended);
        }

        var issued = await _jwtTokensService.IssueTokensAsync(user);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        var renewed = authTokens.RenewTokens(refreshToken, tokens.AccessToken, tokens.RefreshToken,
            tokens.AccessTokenExpiresOn, tokens.RefreshTokenExpiresOn);
        if (renewed.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var saved = await _repository.SaveAsync(authTokens, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        authTokens = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "AuthTokens were refreshed for {Id}", authTokens.Id);
        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.AuthTokensApplication_Refresh_Succeeded,
            "User {Id} succeeded to refresh token", user.Id);
        _recorder.TrackUsageFor(caller.ToCall(), user.Id,
            UsageConstants.Events.UsageScenarios.Generic.UserExtendedLogin,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.AuthProvider, PersonCredentialsApplication.ProviderName },
                { UsageConstants.Properties.UserIdOverride, user.Id }
            });

        return new AuthenticateTokens
        {
            UserId = user.Id,
            AccessToken = new AuthenticationToken
            {
                Value = tokens.AccessToken,
                ExpiresOn = tokens.AccessTokenExpiresOn,
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = tokens.RefreshToken,
                ExpiresOn = tokens.RefreshTokenExpiresOn,
                Type = TokenType.RefreshToken
            }
        };
    }

    public async Task<Result<Error>> RevokeRefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByRefreshTokenAsync(refreshToken, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var authTokens = retrieved.Value.Value;
        var invalidated = authTokens.Revoke(refreshToken);
        if (invalidated.IsFailure)
        {
            return invalidated.Error;
        }

        var saved = await _repository.SaveAsync(authTokens, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        authTokens = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "AuthTokens were revoked for {Id}", authTokens.Id);

        return Result.Ok;
    }
}