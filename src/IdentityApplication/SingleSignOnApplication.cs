using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
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
        var retrievedProvider = await _ssoProvidersService.FindByNameAsync(providerName, cancellationToken);
        if (retrievedProvider.IsFailure)
        {
            return retrievedProvider.Error;
        }

        if (!retrievedProvider.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var provider = retrievedProvider.Value.Value;
        var authenticated = await provider.AuthenticateAsync(caller, authCode, username, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var userInfo = authenticated.Value;
        var existingUser =
            await _endUsersService.FindPersonByEmailPrivateAsync(caller, userInfo.EmailAddress, cancellationToken);
        if (existingUser.IsFailure)
        {
            return existingUser.Error;
        }

        string registeredUserId;
        if (!existingUser.Value.HasValue)
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
            _recorder.AuditAgainst(caller.ToCall(), registeredUserId,
                Audits.SingleSignOnApplication_Authenticate_AccountOnboarded,
                "User {Id} was registered automatically from SSO {Provider}", registeredUserId, providerName);
        }
        else
        {
            registeredUserId = existingUser.Value.Value.Id;
        }

        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, registeredUserId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = retrievedUser.Value;
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
        _recorder.TrackUsageFor(caller.ToCall(), user.Id, UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            user.ToLoginUserUsage(providerName, userInfo));

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

internal static class SingleSignOnApplicationExtensions
{
    public static Dictionary<string, object> ToLoginUserUsage(this EndUserWithMemberships user, string providerName,
        SSOUserInfo userInfo)
    {
        var context = new Dictionary<string, object>
        {
            { UsageConstants.Properties.AuthProvider, providerName },
            { UsageConstants.Properties.UserIdOverride, user.Id },
            { UsageConstants.Properties.Name, userInfo.FullName },
            { UsageConstants.Properties.EmailAddress, userInfo.EmailAddress }
        };

        var defaultMembership = user.Memberships.FirstOrDefault(ms => ms.IsDefault);
        if (defaultMembership.Exists())
        {
            context.Add(UsageConstants.Properties.DefaultOrganizationId, defaultMembership.Id);
        }

        return context;
    }
}