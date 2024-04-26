using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using IdentityApplication.ApplicationServices;

namespace IdentityApplication;

public class SingleSignOnApplication : ISingleSignOnApplication
{
    private readonly IAuthTokensService _authTokensService;
    private readonly IEndUsersService _endUsersService;
    private readonly IRecorder _recorder;
    private readonly ISSOProvidersService _ssoProvidersService;

    public SingleSignOnApplication(IRecorder recorder, IEndUsersService endUsersService,
        ISSOProvidersService ssoProvidersService, IAuthTokensService authTokensService)
    {
        _recorder = recorder;
        _endUsersService = endUsersService;
        _ssoProvidersService = ssoProvidersService;
        _authTokensService = authTokensService;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller,
        string? invitationToken, string providerName,
        string authCode, string? username, CancellationToken cancellationToken)
    {
        var retrieved = await _ssoProvidersService.FindByNameAsync(providerName, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var provider = retrieved.Value.Value;
        var authenticated = await provider.AuthenticateAsync(caller, authCode, username, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var userInfo = authenticated.Value;
        var userExists =
            await _endUsersService.FindPersonByEmailPrivateAsync(caller, userInfo.EmailAddress, cancellationToken);
        if (userExists.IsFailure)
        {
            return userExists.Error;
        }

        string registeredUserId;
        if (!userExists.Value.HasValue)
        {
            var autoRegistered = await _endUsersService.RegisterPersonPrivateAsync(caller, invitationToken,
                userInfo.EmailAddress,
                userInfo.FirstName, userInfo.LastName, userInfo.Timezone.ToString(), userInfo.CountryCode.ToString(),
                true,
                cancellationToken);
            if (autoRegistered.IsFailure)
            {
                return autoRegistered.Error;
            }

            registeredUserId = autoRegistered.Value.Id;

            _recorder.AuditAgainst(caller.ToCall(), autoRegistered.Value.Id,
                Audits.SingleSignOnApplication_Authenticate_AccountOnboarded,
                "User {Id} was registered automatically from SSO {Provider}", autoRegistered.Value.Id, providerName);
        }
        else
        {
            registeredUserId = userExists.Value.Value.Id;
        }

        var registered =
            await _endUsersService.GetMembershipsPrivateAsync(caller, registeredUserId, cancellationToken);
        if (registered.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = registered.Value;
        if (user.Status != EndUserStatus.Registered)
        {
            return Error.NotAuthenticated();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.SingleSignOnApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate with SSO {Provider} with a suspended account", user.Id, providerName);
            return Error.EntityExists(Resources.SingleSignOnApplication_AccountSuspended);
        }

        var saved = await _ssoProvidersService.SaveUserInfoAsync(providerName, registeredUserId.ToId(), userInfo,
            cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.SingleSignOnApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with SSO {Provider}", user.Id, providerName);

        var issued = await _authTokensService.IssueTokensAsync(caller, user, cancellationToken);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        return new Result<AuthenticateTokens, Error>(new AuthenticateTokens
        {
            AccessToken = new AuthenticateToken
            {
                Value = tokens.AccessToken,
                ExpiresOn = tokens.AccessTokenExpiresOn
            },
            RefreshToken = new AuthenticateToken
            {
                Value = tokens.RefreshToken,
                ExpiresOn = tokens.RefreshTokenExpiresOn
            },
            UserId = user.Id
        });
    }
}