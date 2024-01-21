using Application.Common;
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

    public async Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext context, EndUserWithMemberships user,
        CancellationToken cancellationToken)
    {
        var issued = await _jwtTokensService.IssueTokensAsync(user);
        if (!issued.IsSuccessful)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        var retrieved = await _repository.FindByUserIdAsync(user.Id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
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
            if (!root.IsSuccessful)
            {
                return root.Error;
            }

            authTokens = root.Value;
        }

        var set = authTokens.SetTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresOn);
        if (!set.IsSuccessful)
        {
            return set.Error;
        }

        var updated = await _repository.SaveAsync(authTokens, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "AuthTokens were issued for {Id}", updated.Value.Id);

        return tokens;
    }

    public async Task<Result<AccessTokens, Error>> RefreshTokenAsync(ICallerContext context, string refreshToken,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByRefreshTokenAsync(refreshToken, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var authTokens = retrieved.Value.Value;
        var retrievedUser = await _endUsersService.GetMembershipsAsync(context, authTokens.UserId, cancellationToken);
        if (!retrievedUser.IsSuccessful)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        var issued = await _jwtTokensService.IssueTokensAsync(user);
        if (!issued.IsSuccessful)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        var renewed = authTokens.RenewTokens(refreshToken, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresOn);
        if (!renewed.IsSuccessful)
        {
            return Error.NotAuthenticated();
        }

        var updated = await _repository.SaveAsync(authTokens, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "AuthTokens were refreshed for {Id}", updated.Value.Id);

        return tokens;
    }
}