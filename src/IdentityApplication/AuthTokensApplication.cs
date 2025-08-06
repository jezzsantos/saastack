using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using AuthToken = IdentityDomain.AuthToken;

namespace IdentityApplication;

public class AuthTokensApplication : IAuthTokensApplication
{
    private readonly IEncryptionService _encryptionService;
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _idFactory;
    private readonly IJWTTokensService _jwtTokensService;
    private readonly IRecorder _recorder;
    private readonly IAuthTokensRepository _repository;
    private readonly ITokensService _tokensService;
    private readonly IUserProfilesService _userProfilesService;

    public AuthTokensApplication(IRecorder recorder, IIdentifierFactory idFactory, IEncryptionService encryptionService,
        ITokensService tokensService, IJWTTokensService jwtTokensService, IEndUsersService endUsersService,
        IUserProfilesService userProfilesService, IAuthTokensRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _encryptionService = encryptionService;
        _tokensService = tokensService;
        _jwtTokensService = jwtTokensService;
        _endUsersService = endUsersService;
        _userProfilesService = userProfilesService;
        _repository = repository;
    }

    public async Task<Result<AuthenticateTokens, Error>> IssueTokensAsync(ICallerContext caller, string userId,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData, CancellationToken cancellationToken)
    {
        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, userId, cancellationToken);
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

        UserProfile? profile = null;
        if (scopes.HasAny())
        {
            var maintenance = Caller.CreateAsMaintenance(caller);
            var retrievedProfile =
                await _userProfilesService.GetProfilePrivateAsync(maintenance, user.Id, cancellationToken);
            if (retrievedProfile.IsFailure)
            {
                return Error.NotAuthenticated();
            }

            profile = retrievedProfile.Value;
        }

        var issued = await _jwtTokensService.IssueTokensAsync(user, profile, scopes, additionalData);
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
            var root = AuthTokensRoot.Create(_recorder, _idFactory, _encryptionService, _tokensService, user.Id.ToId());
            if (root.IsFailure)
            {
                return root.Error;
            }

            authTokens = root.Value;
        }

        var issuedAccessToken = AuthToken.Create(AuthTokenType.AccessToken, tokens.AccessToken,
            tokens.AccessTokenExpiresOn,
            _encryptionService);
        if (issuedAccessToken.IsFailure)
        {
            return issuedAccessToken.Error;
        }

        var issuedRefreshToken = AuthToken.Create(AuthTokenType.RefreshToken, tokens.RefreshToken,
            tokens.RefreshTokenExpiresOn, _encryptionService);
        if (issuedRefreshToken.IsFailure)
        {
            return issuedRefreshToken.Error;
        }

        var issuedIdToken = Optional<AuthToken>.None;
        if (tokens.IdToken.HasValue())
        {
            var idTokenResult = AuthToken.Create(AuthTokenType.OtherToken, tokens.IdToken!, tokens.IdTokenExpiresOn,
                _encryptionService);
            if (idTokenResult.IsFailure)
            {
                return idTokenResult.Error;
            }

            issuedIdToken = idTokenResult.Value.ToOptional();
        }

        var set = authTokens.SetTokens(issuedAccessToken.Value, issuedRefreshToken.Value, issuedIdToken);
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

        return tokens.ToTokens(user.Id);
    }

    public async Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext caller, string refreshToken,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData, CancellationToken cancellationToken)
    {
        var refreshTokenDigest = _tokensService.CreateTokenDigest(refreshToken);
        var retrieved = await _repository.FindByRefreshTokenDigestAsync(refreshTokenDigest, cancellationToken);
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

        UserProfile? profile = null;
        if (scopes.HasAny())
        {
            var maintenance = Caller.CreateAsMaintenance(caller);
            var retrievedProfile =
                await _userProfilesService.GetProfilePrivateAsync(maintenance, user.Id, cancellationToken);
            if (retrievedProfile.IsFailure)
            {
                return Error.NotAuthenticated();
            }

            profile = retrievedProfile.Value;
        }

        var issued = await _jwtTokensService.IssueTokensAsync(user, profile, scopes, additionalData);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var tokens = issued.Value;

        var issuedAccessToken = AuthToken.Create(AuthTokenType.AccessToken, tokens.AccessToken,
            tokens.AccessTokenExpiresOn,
            _encryptionService);
        if (issuedAccessToken.IsFailure)
        {
            return issuedAccessToken.Error;
        }

        var issuedRefreshToken = AuthToken.Create(AuthTokenType.RefreshToken, tokens.RefreshToken,
            tokens.RefreshTokenExpiresOn, _encryptionService);
        if (issuedRefreshToken.IsFailure)
        {
            return issuedRefreshToken.Error;
        }

        var issuedIdToken = Optional<AuthToken>.None;
        if (tokens.IdToken.HasValue())
        {
            var idTokenResult = AuthToken.Create(AuthTokenType.OtherToken, tokens.IdToken!, tokens.IdTokenExpiresOn,
                _encryptionService);
            if (idTokenResult.IsFailure)
            {
                return idTokenResult.Error;
            }

            issuedIdToken = idTokenResult.Value.ToOptional();
        }

        var renewed = authTokens.RenewTokens(refreshToken, issuedAccessToken.Value, issuedRefreshToken.Value,
            issuedIdToken);
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
                { UsageConstants.Properties.AuthProvider, NativeIdentityServerCredentialsService.ProviderName },
                { UsageConstants.Properties.UserIdOverride, user.Id }
            });

        return tokens.ToTokens(user.Id);
    }

    public async Task<Result<Error>> RevokeRefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken)
    {
        var refreshTokenDigest = _tokensService.CreateTokenDigest(refreshToken);
        var retrieved = await _repository.FindByRefreshTokenDigestAsync(refreshTokenDigest, cancellationToken);
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

public static class AuthTokensApplicationConversionExtensions
{
    public static AuthenticateTokens ToTokens(this AccessTokens tokens, string userId)
    {
        return new AuthenticateTokens
        {
            UserId = userId,
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
            },
            IdToken = tokens.IdToken.Exists()
                ? new AuthenticationToken
                {
                    Value = tokens.IdToken!,
                    ExpiresOn = tokens.IdTokenExpiresOn,
                    Type = TokenType.OtherToken
                }
                : null
        };
    }
}