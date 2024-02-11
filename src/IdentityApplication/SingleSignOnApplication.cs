using Application.Common;
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

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext context, string providerName,
        string authCode, string? username, CancellationToken cancellationToken)
    {
        var retrieved = await _ssoProvidersService.FindByNameAsync(providerName, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var provider = retrieved.Value.Value;
        var authenticated = await provider.AuthenticateAsync(context, authCode, username, cancellationToken);
        if (!authenticated.IsSuccessful)
        {
            return authenticated.Error;
        }

        var userInfo = authenticated.Value;
        var userExists =
            await _endUsersService.FindPersonByEmailAsync(context, userInfo.EmailAddress, cancellationToken);
        if (!userExists.IsSuccessful)
        {
            return userExists.Error;
        }

        string registeredUserId;
        if (!userExists.Value.HasValue)
        {
            var autoRegistered = await _endUsersService.RegisterPersonAsync(context, userInfo.EmailAddress,
                userInfo.FirstName, userInfo.LastName, userInfo.Timezone.ToString(), userInfo.CountryCode.ToString(),
                true,
                cancellationToken);
            if (!autoRegistered.IsSuccessful)
            {
                return autoRegistered.Error;
            }

            registeredUserId = autoRegistered.Value.Id;

            _recorder.AuditAgainst(context.ToCall(), autoRegistered.Value.Id,
                Audits.SingleSignOnApplication_Authenticate_AccountOnboarded,
                "User {Id} was registered automatically from SSO {Provider}", autoRegistered.Value.Id, providerName);
        }
        else
        {
            registeredUserId = userExists.Value.Value.Id;
        }

        var registered = await _endUsersService.GetMembershipsAsync(context, registeredUserId, cancellationToken);
        if (!registered.IsSuccessful)
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
            _recorder.AuditAgainst(context.ToCall(), user.Id,
                Audits.SingleSignOnApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate with SSO {Provider} with a suspended account", user.Id, providerName);
            return Error.EntityExists(Resources.SingleSignOnApplication_AccountSuspended);
        }

        var saved = await _ssoProvidersService.SaveUserInfoAsync(providerName, registeredUserId.ToId(), userInfo,
            cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.AuditAgainst(context.ToCall(), user.Id,
            Audits.SingleSignOnApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with SSO {Provider}", user.Id, providerName);

        var issued = await _authTokensService.IssueTokensAsync(context, user, cancellationToken);
        if (!issued.IsSuccessful)
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