using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides an adapter to the UserPilot.com service
///     <see href="https://docs.userpilot.com/article/195-identify-users-and-track-api" />
///     In UserPilot, a user is assumed to be unique across all companies, which means that a unique user belongs to a
///     unique company. A unique user cannot belong to two different companies at the same time (also they cannot be
///     removed from a company).
///     Thus, when we identify a user, we need to use their userId@organizationId as their unique identifier.
///     Certain events will "identify" users to UserPilot, and we will these moments to set the user's details,
///     and set their company's details:
///     * UserLogin - identify/create the platform-user and default-tenant-user with their email and name (2x calls)
///     * PersonRegistrationCreated/MachineRegistered - identify/create the platform-user with their email and name
///     * UserProfileChanged - identify/create the platform-user and default-tenant-user and change the email and name of
///     both (2x calls)
///     * OrganizationCreated - identify/create the tenant-user and create the company with its name
///     * OrganizationChanged - identify/create the platform-user and change their company's name
///     * MembershipAdded - identify/create the tenant-user and change their company
///     * MembershipChanged - identify/create the tenanted-user and change their email and name (of their "changed"
///     organization)
/// </summary>
public sealed class UserPilotHttpServiceClient : IUsageDeliveryService
{
    internal const string CompanyIdPropertyName = "id";
    internal const string CompanyNamePropertyName = "name";
    internal const string CreatedAtPropertyName = "created_at";
    internal const string UnTenantedValue = "platform";
    internal const string UserEmailAddressPropertyName = "email";
    internal const string UserNamePropertyName = "name";
    private const string AnonymousUserId = "anonymous";
    private const string UserIdDelimiter = "@";
    private static readonly string[] IgnoredCustomEventProperties =
    [
        UsageConstants.Properties.UserIdOverride,
        UsageConstants.Properties.TenantIdOverride,
        UsageConstants.Properties.DefaultOrganizationId
    ];
    private readonly IRecorder _recorder;
    private readonly IUserPilotClient _serviceClient;

    public UserPilotHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new UserPilotClient(recorder, settings, httpClientFactory))
    {
    }

    internal UserPilotHttpServiceClient(IRecorder recorder, IUserPilotClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default)
    {
        var options = DetermineOptions(caller, forId, eventName, additional);
        var userId = DetermineUserId(options, additional);
        var isIdentifiableEvent = IsReIdentifiableEvent(eventName, additional);
        if (isIdentifiableEvent)
        {
            var identified = await IdentifyUserAsync(caller, userId, eventName, additional, cancellationToken);
            if (identified.IsFailure)
            {
                return identified.Error;
            }
        }

        var trackedMetadata = CreateTrackedEventProperties(options, eventName, additional);
        var tracked = await TrackEventAsync(caller, userId, eventName, trackedMetadata, cancellationToken);
        if (tracked.IsFailure)
        {
            return tracked.Error;
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged)
        {
            if (!isIdentifiableEvent)
            {
                return Result.Ok;
            }

            if (additional.NotExists()
                || !additional.TryGetValue(UsageConstants.Properties.DefaultOrganizationId,
                    out var defaultOrganizationId))
            {
                return Result.Ok;
            }

            options.ResetTenantIdOverride(defaultOrganizationId);
            var secondUserId = DetermineUserId(options, additional);
            var secondTrackedMetadata = CreateTrackedEventProperties(options, eventName, additional);

            var identifiedTenant =
                await IdentifyUserAsync(caller, secondUserId, eventName, additional, cancellationToken);
            if (identifiedTenant.IsFailure)
            {
                return identifiedTenant.Error;
            }

            var trackedTenant =
                await TrackEventAsync(caller, secondUserId, eventName, secondTrackedMetadata, cancellationToken);
            if (trackedTenant.IsFailure)
            {
                return trackedTenant.Error;
            }
        }

        return Result.Ok;
    }

    private static ContextOptions DetermineOptions(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional)
    {
        string? tenantIdOverride = null;
        switch (eventName)
        {
            case UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated:
            case UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged:
                tenantIdOverride = additional.Exists()
                                   && additional.TryGetValue(UsageConstants.Properties.Id,
                                       out var organizationId)
                    ? organizationId
                    : null;
                break;
        }

        return new ContextOptions(
            forId,
            caller.TenantId.HasValue()
                ? caller.TenantId
                : UnTenantedValue,
            tenantIdOverride);
    }

    private static bool IsReIdentifiableEvent(string eventName, Dictionary<string, string>? additional)
    {
        if (additional.NotExists())
        {
            return false;
        }

        // Updates the user details
        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin)
        {
            return additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out _);
        }

        // Updates the email or name of a user
        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered)
        {
            return additional.TryGetValue(UsageConstants.Properties.Id, out _);
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged)
        {
            return additional.TryGetValue(UsageConstants.Properties.Id, out _)
                   && (additional.TryGetValue(UsageConstants.Properties.Name, out _)
                       || additional.TryGetValue(UsageConstants.Properties.EmailAddress, out _));
        }

        // Updates the company details
        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated
            or UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged)
        {
            return additional.TryGetValue(UsageConstants.Properties.Id, out _);
        }

        // Updates the company details and user details
        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipAdded
            or UsageConstants.Events.UsageScenarios.Generic.MembershipChanged)
        {
            return additional.TryGetValue(UsageConstants.Properties.Id, out _)
                   && additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out _);
        }

        return false;
    }

    private async Task<Result<Error>> IdentifyUserAsync(ICallerContext caller, string userId,
        string eventName, Dictionary<string, string>? additional, CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Identifying user in UserPilot for {User}", userId);

        var userMetadata = CreateIdentifiedUserProperties(eventName, additional);
        var companyMetadata = CreateIdentifiedCompanyProperties(eventName, additional);
        var identified =
            await _serviceClient.IdentifyUserAsync(caller.ToCall(), userId, userMetadata, companyMetadata,
                cancellationToken);
        if (identified.IsFailure)
        {
            return identified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Identified user in UserPilot for {User} successfully",
            userId);

        return Result.Ok;
    }

    private async Task<Result<Error>> TrackEventAsync(ICallerContext caller, string userId,
        string eventName, Dictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Tracking event {Event} in UserPilot for {User}", eventName,
            userId);

        var tracked =
            await _serviceClient.TrackEventAsync(caller.ToCall(), userId, eventName, metadata, cancellationToken);
        if (tracked.IsFailure)
        {
            return tracked.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Tracked event {Event} in UserPilot for {User} successfully",
            eventName, userId);

        return Result.Ok;
    }

    private static Dictionary<string, string> CreateIdentifiedUserProperties(string eventName,
        Dictionary<string, string>? additional)
    {
        var metadata = new Dictionary<string, string>();
        if (additional.NotExists())
        {
            return metadata;
        }

        if (eventName is UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered
            or UsageConstants.Events.UsageScenarios.Generic.MembershipAdded)
        {
            var now = DateTime.UtcNow.ToNearestSecond().ToIso8601();
            metadata.TryAdd(CreatedAtPropertyName, ConvertToUserPilotDataType(now));
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged
            or UsageConstants.Events.UsageScenarios.Generic.MembershipChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.EmailAddress, out var emailAddress))
            {
                metadata.TryAdd(UserEmailAddressPropertyName, ConvertToUserPilotDataType(emailAddress));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(UserNamePropertyName, ConvertToUserPilotDataType(name));
            }
        }

        return metadata;
    }

    private static Dictionary<string, string> CreateIdentifiedCompanyProperties(string eventName,
        Dictionary<string, string>? additional)
    {
        var metadata = new Dictionary<string, string>();
        if (additional.NotExists())
        {
            return metadata;
        }

        if (eventName is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated)
        {
            var now = DateTime.UtcNow.ToNearestSecond().ToIso8601();
            metadata.TryAdd(CreatedAtPropertyName, ConvertToUserPilotDataType(now));
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated
            or UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.Id, out var companyId))
            {
                metadata.TryAdd(CompanyIdPropertyName, ConvertToUserPilotDataType(companyId));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(CompanyNamePropertyName, ConvertToUserPilotDataType(name));
            }
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipAdded
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var companyId))
            {
                metadata.TryAdd(CompanyIdPropertyName, ConvertToUserPilotDataType(companyId));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(CompanyNamePropertyName, ConvertToUserPilotDataType(name));
            }
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var companyId))
            {
                metadata.TryAdd(CompanyIdPropertyName, ConvertToUserPilotDataType(companyId));
            }
        }

        return metadata;
    }

    private static Dictionary<string, string> CreateTrackedEventProperties(ContextOptions options, string eventName,
        Dictionary<string, string>? additional)
    {
        var metadata = new Dictionary<string, string>();

        var tenantId = DetermineTenantId(options, additional);
        metadata.TryAdd(UsageConstants.Properties.TenantId, tenantId);

        if (additional.NotExists())
        {
            return metadata;
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out var overriddenUserId))
            {
                metadata.TryAdd(UsageConstants.Properties.Id, ConvertToUserPilotDataType(overriddenUserId));
            }
        }

        foreach (var pair in additional.Where(
                     pair => IgnoredCustomEventProperties.NotContainsIgnoreCase(pair.Key)))
        {
            metadata.TryAdd(pair.Key, ConvertToUserPilotDataType(pair.Value));
        }

        return metadata;
    }

    /// <summary>
    ///     UserPilot only supports strings, where: datetime is in UnixSeconds
    /// </summary>
    private static string ConvertToUserPilotDataType(string value)
    {
        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime.ToUnixSeconds().ToString();
        }

        return value;
    }

    private static string DetermineUserId(ContextOptions options, Dictionary<string, string>? additional)
    {
        var tenantId = DetermineTenantId(options, additional);
        return DetermineUserId(options, additional, tenantId);
    }

    private static string DetermineUserId(ContextOptions options, Dictionary<string, string>? additional,
        string tenantId)
    {
        var userId = options.ForId;
        if (additional.Exists())
        {
            if (additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out var overriddenUserId))
            {
                userId = overriddenUserId;
            }
        }

        if (userId.EqualsIgnoreCase(CallerConstants.AnonymousUserId))
        {
            userId = AnonymousUserId;
        }

        return $"{userId}{UserIdDelimiter}{tenantId}";
    }

    private static string DetermineTenantId(ContextOptions options, Dictionary<string, string>? additional)
    {
        if (additional.Exists())
        {
            if (options.TenantIdOverride.HasValue())
            {
                return options.TenantIdOverride;
            }

            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var overriddenTenantId))
            {
                return overriddenTenantId;
            }

            if (additional.TryGetValue(UsageConstants.Properties.TenantId, out var specifiedTenantId))
            {
                return specifiedTenantId;
            }
        }

        return options.TenantId;
    }

    private sealed class ContextOptions
    {
        public ContextOptions(string forId, string tenantId, string? tenantIdOverride)
        {
            ForId = forId;
            TenantId = tenantId;
            TenantIdOverride = tenantIdOverride;
        }

        public string ForId { get; }

        public string TenantId { get; }

        public string? TenantIdOverride { get; private set; }

        public void ResetTenantIdOverride(string tenantId)
        {
            TenantIdOverride = tenantId;
        }
    }
}