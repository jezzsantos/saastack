using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides an adapter to the UserPilot.com service
///     <see href="https://docs.userpilot.com/article/195-identify-users-and-track-api" />
///     Note: In UserPilot, users are not unique across all companies, which means that a unique user belongs to a
///     unique company. A unique user cannot belong to two different companies at the same time (also they cannot be
///     removed from a company). Thus, each user's identity is specific to a specific company /tenant.
///     Thus, when we identify a user, we need to use their userId@tenantId as their unique identifier.
///     Certain events will require us to send two usage events, one as the platform user and one as the tenant user.
///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.UserLogin" /> AND
///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged" />
///     UserPilot does not really support much of a schema for events and their properties, so we can create whatever we
///     like.
/// </summary>
public sealed class UserPilotHttpServiceClient : IUsageDeliveryService
{
    private static readonly string[] IgnoredCustomEventProperties =
    {
        UsageConstants.Properties.Timestamp,
        UsageConstants.Properties.TenantId,
        UsageConstants.Properties.CallId,
        UsageDeliveryTranslator.BrowserReferrer, UsageConstants.Properties.ReferredBy,
        UsageConstants.Properties.Path,
        UsageDeliveryTranslator.BrowserIp, UsageConstants.Properties.IpAddress,
        UsageConstants.Properties.UserAgent,
        UserPilotConstants.MetadataProperties.ReferredBy,
        UserPilotConstants.MetadataProperties.IpAddress,
        UserPilotConstants.MetadataProperties.Url
    };
    private readonly IRecorder _recorder;
    private readonly IUserPilotClient _serviceClient;
    private readonly IUsageDeliveryTranslator _translator;

    public UserPilotHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new UserPilotClient(recorder, settings, httpClientFactory))
    {
    }

    internal UserPilotHttpServiceClient(IRecorder recorder, IUserPilotClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _translator = new UsageDeliveryTranslator();
    }

    public async Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default)
    {
        _translator.StartTranslation(caller, forId, eventName, additional, true);
        var userId = _translator.UserId;
        var isIdentifiableEvent = _translator.IsUserIdentifiableEvent();
        if (isIdentifiableEvent)
        {
            var identified = await IdentifyUserAsync(caller, userId, eventName, additional, cancellationToken);
            if (identified.IsFailure)
            {
                return identified.Error;
            }
        }

        var eventProperties = _translator.PrepareProperties(false, ConvertToUserPilotDataType);
        var tracked = await TrackEventAsync(caller, userId, eventName, eventProperties, cancellationToken);
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

            var secondEventProperties = _translator.PrepareProperties(true, ConvertToUserPilotDataType);
            var secondUserId = _translator.UserId;

            var identifiedTenant =
                await IdentifyUserAsync(caller, secondUserId, eventName, additional, cancellationToken);
            if (identifiedTenant.IsFailure)
            {
                return identifiedTenant.Error;
            }

            var secondTracked =
                await TrackEventAsync(caller, secondUserId, eventName, secondEventProperties, cancellationToken);
            if (secondTracked.IsFailure)
            {
                return secondTracked.Error;
            }
        }

        return Result.Ok;
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
        var properties = CalculateTrackingProperties(caller, metadata);
        _recorder.TraceInformation(caller.ToCall(), "Tracking event {Event} in UserPilot for {User}", eventName,
            userId);

        var tracked =
            await _serviceClient.TrackEventAsync(caller.ToCall(), userId, eventName, properties, cancellationToken);
        if (tracked.IsFailure)
        {
            return tracked.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Tracked event {Event} in UserPilot for {User} successfully",
            eventName, userId);

        return Result.Ok;
    }

    private Dictionary<string, string> CalculateTrackingProperties(ICallerContext caller,
        Dictionary<string, string> metadata)
    {
        var properties = new Dictionary<string, string>();
        var browserProperties = _translator.GetBrowserProperties(metadata);
        if (browserProperties.Referrer.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.ReferredBy,
                ConvertToUserPilotDataType(browserProperties.Referrer));
        }

        if (browserProperties.Url.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.Url,
                ConvertToUserPilotDataType(browserProperties.Url));
        }

        if (browserProperties.IpAddress.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.IpAddress,
                ConvertToUserPilotDataType(browserProperties.IpAddress));
        }

        var userAgent = metadata.GetValueOrDefault(UsageConstants.Properties.UserAgent);
        var components = _translator.GetUserAgentProperties(userAgent);
        if (components.Browser.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.Browser,
                ConvertToUserPilotDataType(components.Browser));
        }

        if (components.BrowserVersion.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.BrowserVersion,
                ConvertToUserPilotDataType(components.BrowserVersion));
        }

        if (components.OperatingSystem.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.OperatingSystem,
                ConvertToUserPilotDataType(components.OperatingSystem));
        }

        if (components.Device.HasValue())
        {
            properties.Add(UserPilotConstants.MetadataProperties.Device,
                ConvertToUserPilotDataType(components.Device));
        }

        var tenantId = metadata.GetValueOrDefault(UsageConstants.Properties.TenantId);
        properties.Add(UserPilotConstants.MetadataProperties.TenantId, tenantId!);
        var callId = metadata.GetValueOrDefault(UsageConstants.Properties.CallId) ?? caller.CallId;
        properties.Add(UserPilotConstants.MetadataProperties.CallId, callId);

        var additionalProperties = metadata
            .Where(pair => IgnoredCustomEventProperties.NotContainsIgnoreCase(pair.Key));
        foreach (var pair in additionalProperties)
        {
            properties.Add(pair.Key, ConvertToUserPilotDataType(pair.Value));
        }

        return properties;
    }

    private Dictionary<string, string> CreateIdentifiedUserProperties(string eventName,
        Dictionary<string, string>? additional)
    {
        var components = _translator.GetUserProperties(eventName, additional);
        var metadata = new Dictionary<string, string>();
        if (components.CreatedAt.HasValue())
        {
            metadata.TryAdd(UserPilotConstants.MetadataProperties.CreatedAtPropertyName,
                ConvertToUserPilotDataType(components.CreatedAt.ToIso8601()));
        }

        if (components.Name.HasValue())
        {
            metadata.TryAdd(UserPilotConstants.MetadataProperties.UserNamePropertyName,
                ConvertToUserPilotDataType(components.Name));
        }

        if (components.EmailAddress.HasValue())
        {
            metadata.TryAdd(UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName,
                ConvertToUserPilotDataType(components.EmailAddress));
        }

        if (components.Timezone.HasValue())
        {
            metadata.TryAdd(UserPilotConstants.MetadataProperties.UserTimezonePropertyName,
                ConvertToUserPilotDataType(components.Timezone));
        }

        if (components.CountryCode.HasValue())
        {
            metadata.TryAdd(UserPilotConstants.MetadataProperties.UserCountryCodePropertyName,
                ConvertToUserPilotDataType(components.CountryCode));
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
            metadata.TryAdd(UserPilotConstants.MetadataProperties.CreatedAtPropertyName,
                ConvertToUserPilotDataType(now));
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated
            or UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.Id, out var companyId))
            {
                metadata.TryAdd(UserPilotConstants.MetadataProperties.CompanyIdPropertyName,
                    ConvertToUserPilotDataType(companyId));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(UserPilotConstants.MetadataProperties.CompanyNamePropertyName,
                    ConvertToUserPilotDataType(name));
            }
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipAdded
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var companyId))
            {
                metadata.TryAdd(UserPilotConstants.MetadataProperties.CompanyIdPropertyName,
                    ConvertToUserPilotDataType(companyId));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(UserPilotConstants.MetadataProperties.CompanyNamePropertyName,
                    ConvertToUserPilotDataType(name));
            }
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var companyId))
            {
                metadata.TryAdd(UserPilotConstants.MetadataProperties.CompanyIdPropertyName,
                    ConvertToUserPilotDataType(companyId));
            }
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
}