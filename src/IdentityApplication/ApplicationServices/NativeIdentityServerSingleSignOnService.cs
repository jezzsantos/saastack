using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native SSO service for authenticating with 3rd party providers and managing and persisting their tokens
/// </summary>
public class NativeIdentityServerSingleSignOnService : IIdentityServerSingleSignOnService
{
    public const string AuthErrorProviderName = "provider";
    private readonly IAuthTokensService _authTokensService;
    private readonly IEndUsersService _endUsersService;
    private readonly IRecorder _recorder;
    private readonly ISSOProvidersService _ssoProvidersService;

    public NativeIdentityServerSingleSignOnService(IRecorder recorder, IEndUsersService endUsersService,
        ISSOProvidersService ssoProvidersService, IAuthTokensService authTokensService)
    {
        _recorder = recorder;
        _endUsersService = endUsersService;
        _ssoProvidersService = ssoProvidersService;
        _authTokensService = authTokensService;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller,
        string? invitationToken, string providerName, string authCode, string? codeVerifier, string? username,
        bool? termsAndConditionsAccepted, CancellationToken cancellationToken)
    {
        var authenticated =
            await _ssoProvidersService.AuthenticateUserAsync(caller, providerName, authCode, codeVerifier, username,
                cancellationToken);
        if (authenticated.IsFailure)
        {
            if (authenticated.Error.Is(ErrorCode.NotAuthenticated)
                || authenticated.Error.Is(ErrorCode.EntityNotFound)
                || authenticated.Error.Is(ErrorCode.Validation))
            {
                return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
            }

            return authenticated.Error;
        }

        //Have we seen you before? based on ProviderName+(OID+TID) (e.g. Microsoft's unique ID for a user, unique in a MS tenant?)
        var authUserInfo = authenticated.Value;
        var existingUser =
            await _ssoProvidersService.FindUserByProviderAsync(caller, providerName, authUserInfo, cancellationToken);
        if (existingUser.IsFailure)
        {
            return existingUser.Error;
        }

        string registeredUserId;
        if (!existingUser.Value.HasValue)
        {
            var autoRegistered = await _endUsersService.RegisterPersonPrivateAsync(caller, invitationToken,
                authUserInfo.EmailAddress, authUserInfo.FirstName, authUserInfo.LastName,
                authUserInfo.Timezone.ToString(), authUserInfo.Locale.ToString(), authUserInfo.CountryCode.ToString(),
                !termsAndConditionsAccepted.HasValue || termsAndConditionsAccepted.Value,
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
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        if (user.Status != EndUserStatus.Registered)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.SingleSignOnApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate with SSO {Provider} with a suspended account", user.Id, providerName);
            return Error.EntityLocked(Resources.SingleSignOnApplication_AccountSuspended);
        }

        var saved = await _ssoProvidersService.SaveInfoOnBehalfOfUserAsync(caller, providerName,
            registeredUserId.ToId(), authUserInfo, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var issued = await _authTokensService.IssueTokensAsync(caller, user.Id, null, null, cancellationToken);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Authentication granted for {UserId} from {Provider}",
            user.Id, providerName);
        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.SingleSignOnApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with SSO {Provider}", user.Id, providerName);
        _recorder.TrackUsageFor(caller.ToCall(), user.Id, UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            user.ToLoginUserUsage(providerName, authUserInfo));

        return issued.Value;
    }

    public async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensForUserAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        return await _ssoProvidersService.GetTokensForUserAsync(caller, userId, cancellationToken);
    }

    public async Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenForUserAsync(ICallerContext caller,
        string userId, string providerName, string refreshToken, CancellationToken cancellationToken)
    {
        var retrievedProvider =
            await _ssoProvidersService.FindProviderByUserIdAsync(caller, userId, providerName, cancellationToken);
        if (retrievedProvider.IsFailure)
        {
            return retrievedProvider.Error;
        }

        if (!retrievedProvider.Value.HasValue)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        var provider = retrievedProvider.Value.Value;
        var retrievedUser =
            await _endUsersService.GetUserPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.SingleSignOnApplication_Refresh_AccountSuspended,
                "User {Id} tried to refresh tokens with SSO {Provider} with a suspended account", user.Id,
                providerName);
            return Error.EntityExists(Resources.SingleSignOnApplication_AccountSuspended);
        }

        var refreshed = await provider.RefreshTokenAsync(caller, refreshToken, cancellationToken);
        if (refreshed.IsFailure)
        {
            return Error.NotAuthenticated(additionalData: GetAuthenticationErrorData(providerName));
        }

        var tokens = refreshed.Value;
        var saved = await _ssoProvidersService.SaveTokensOnBehalfOfUserAsync(caller, providerName, userId, tokens,
            cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.SingleSignOnApplication_Refresh_Succeeded,
            "User {Id} succeeded to refresh token with SSO {Provider}", user.Id, providerName);
        _recorder.TrackUsageFor(caller.ToCall(), user.Id,
            UsageConstants.Events.UsageScenarios.Generic.UserExtendedLogin,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.AuthProvider, providerName },
                { UsageConstants.Properties.UserIdOverride, user.Id }
            });

        return tokens;
    }

    private static Dictionary<string, object> GetAuthenticationErrorData(string providerName)
    {
        return new Dictionary<string, object> { { AuthErrorProviderName, providerName } };
    }
}

internal static class NativeIdentityServerSingleSignOnServiceConversionExtensions
{
    public static Dictionary<string, object> ToLoginUserUsage(this EndUserWithMemberships user, string providerName,
        SSOAuthUserInfo authUserInfo)
    {
        var context = new Dictionary<string, object>
        {
            { UsageConstants.Properties.AuthProvider, providerName },
            { UsageConstants.Properties.UserIdOverride, user.Id },
            { UsageConstants.Properties.Name, authUserInfo.FullName },
            { UsageConstants.Properties.EmailAddress, authUserInfo.EmailAddress }
        };

        var defaultMembership = user.Memberships.FirstOrDefault(ms => ms.IsDefault);
        if (defaultMembership.Exists())
        {
            context.Add(UsageConstants.Properties.DefaultOrganizationId, defaultMembership.OrganizationId);
        }

        return context;
    }
}