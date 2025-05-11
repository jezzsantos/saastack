using System.Collections;
using System.Text.Json;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;
using UAParser;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides an adapter to the Mixpanel.com service
///     <see href="https://developer.mixpanel.com/reference/overview" />
///     In Mixpanel, a user is assumed to be unique across all companies, which means that a unique user belongs to a
///     unique company. A unique user cannot belong to two different companies at the same time (also they cannot be
///     removed from a company).
///     Thus, when we identify a user, we need to use their userId@tenantId as their unique identifier.
///     Certain events will "identify" users to Mixpanel, and we will these moments to set the user's details,
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
public class MixpanelHttpServiceClient : IUsageDeliveryService
{
    internal static readonly string[]
        ForbiddenMixpanelDistinctIds = //See: https://developer.mixpanel.com/reference/import-events
        {
            "00000000-0000-0000-0000-000000000000",
            "anon",
            "anonymous",
            "nil",
            "none",
            "null",
            "n/a",
            "na",
            "undefined",
            "unknown",
            "<nil>",
            "0",
            "-1",
            "true",
            "false",
            "[]",
            "{}"
        };
    private static readonly string[] IgnoredCustomEventProperties =
    {
        UsageConstants.Properties.Timestamp,
        UsageConstants.Properties.TenantId,
        UsageConstants.Properties.CallId,
        "Referrer", UsageConstants.Properties.ReferredBy,
        UsageConstants.Properties.Path,
        "Ip", UsageConstants.Properties.IpAddress,
        UsageConstants.Properties.UserAgent,
        nameof(MixpanelEventProperties.DistinctId).ToSnakeCase(),
        nameof(MixpanelEventProperties.Time).ToSnakeCase(),
        "Referred by",
        nameof(MixpanelEventProperties.Ip).ToSnakeCase(),
        nameof(MixpanelEventProperties.Url).ToSnakeCase()
    };
    private readonly IRecorder _recorder;
    private readonly IMixpanelClient _serviceClient;
    private readonly IUsageDeliveryTranslator _translator;

    public MixpanelHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new MixpanelClient(recorder, settings, httpClientFactory, JsonSerializerOptions.Default))
    {
    }

    internal MixpanelHttpServiceClient(IRecorder recorder, IMixpanelClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _translator = new UsageDeliveryTranslator();
    }

    public async Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default)
    {
        _translator.StartTranslation(caller, forId, eventName, additional);
        var userId = _translator.UserId;
        var isIdentifiableEvent = _translator.IsUserIdentifiableEvent();
        if (isIdentifiableEvent)
        {
            var identified = await SetProfileAsync(caller, userId, additional, cancellationToken);
            if (identified.IsFailure)
            {
                return identified.Error;
            }
        }

        var eventProperties = _translator.PrepareProperties(value => value);
        var imported = await ImportEventAsync(caller, userId, eventName, eventProperties, cancellationToken);
        if (imported.IsFailure)
        {
            return imported;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> ImportEventAsync(ICallerContext caller, string userId, string eventName,
        Dictionary<string, string> eventProperties, CancellationToken cancellationToken)
    {
        var properties = CalculateImportProperties(caller, userId, eventProperties);

        _recorder.TraceInformation(caller.ToCall(), "Importing event {Event} in Mixpanel for {User}",
            eventName, userId);

        var imported =
            await _serviceClient.ImportAsync(caller.ToCall(), userId, eventName, properties, cancellationToken);
        if (imported.IsFailure)
        {
            return imported.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Imported event {Event} in Mixpanel for {User} successfully",
            eventName, userId);

        return Result.Ok;
    }

    private async Task<Result<Error>> SetProfileAsync(ICallerContext caller, string userId,
        Dictionary<string, string>? additional, CancellationToken cancellationToken)
    {
        var properties = CalculateProfileProperties(additional);

        _recorder.TraceInformation(caller.ToCall(), "Setting profile in Mixpanel for {User}", userId);

        var profiled = await _serviceClient.SetProfileAsync(caller.ToCall(), userId, properties, cancellationToken);
        if (profiled.IsFailure)
        {
            return profiled.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Set profile in Mixpanel for {User} successfully",
            userId);

        return Result.Ok;
    }

    private static MixpanelProfileProperties CalculateProfileProperties(Dictionary<string, string>? additional)
    {
        var properties = new MixpanelProfileProperties
        {
            Unsubscribed = true,
            Name = additional?.GetValueOrDefault(UsageConstants.Properties.Name),
            Email = additional?.GetValueOrDefault(UsageConstants.Properties.EmailAddress),
            Timezone = additional?.GetValueOrDefault(UsageConstants.Properties.Timezone),
            Avatar = additional?.GetValueOrDefault(UsageConstants.Properties.AvatarUrl),
            CountryCode = additional?.GetValueOrDefault(UsageConstants.Properties.CountryCode)
        };

        return properties;
    }

    private static MixpanelEventProperties CalculateImportProperties(ICallerContext caller,
        string userId, Dictionary<string, string> eventProperties)
    {
        var distinctId = SanitizeDistinctId(userId);
        var time = GetTimestamp(eventProperties);
        var properties = new MixpanelEventProperties
        {
            Time = time,
            DistinctId = distinctId,
            InsertId = caller.CallId,
            ReferredBy = eventProperties.GetValueOrDefault("Referrer") ??
                         eventProperties.GetValueOrDefault(UsageConstants.Properties.ReferredBy),
            Url = eventProperties.GetValueOrDefault(UsageConstants.Properties.Path),
            Ip = eventProperties.GetValueOrDefault("Ip") ??
                 eventProperties.GetValueOrDefault(UsageConstants.Properties.IpAddress)
        };

        var tenantId = eventProperties.GetValueOrDefault(UsageConstants.Properties.TenantId);
        properties.Add(nameof(ICallContext.TenantId), tenantId);
        var callId = eventProperties.GetValueOrDefault(UsageConstants.Properties.CallId) ??
                     caller.CallId;
        properties.Add(nameof(ICallContext.CallId), callId);
        var userAgent = eventProperties.GetValueOrDefault(UsageConstants.Properties.UserAgent);
        if (userAgent.HasValue())
        {
            var components = ConvertUserAgentToMixpanelProperties(userAgent);
            foreach (var pair in components)
            {
                if (pair.Value.Exists())
                {
                    properties.Add(pair.Key, ConvertToMixpanelDataType(pair.Value));
                }
            }
        }

        var additionalProperties = eventProperties
            .Where(pair => IgnoredCustomEventProperties.NotContainsIgnoreCase(pair.Key));
        foreach (var pair in additionalProperties)
        {
            properties.Add(pair.Key, ConvertToMixpanelDataType(pair.Value));
        }

        return properties;
    }

    private static long GetTimestamp(Dictionary<string, string>? additional)
    {
        if (additional.NotExists())
        {
            return DateTime.UtcNow.ToUnixSeconds();
        }

        return additional.TryGetValue(UsageConstants.Properties.Timestamp, out var time)
            ? time.FromIso8601().ToUnixSeconds()
            : DateTime.UtcNow.ToUnixSeconds();
    }

    /// <summary>
    ///     Mixpanel forbids the use of certain strings for the <see cref="MixpanelEventProperties.DistinctId" />
    ///     See: https://developer.mixpanel.com/reference/import-events
    /// </summary>
    private static string SanitizeDistinctId(string distinctId)
    {
        if (distinctId.HasNoValue())
        {
            return string.Empty;
        }

        return ForbiddenMixpanelDistinctIds.ContainsIgnoreCase(distinctId)
            ? string.Empty
            : distinctId;
    }

    /// <summary>
    ///     Mixpanel only supports these data types: string, number, datetime (in ISO8601), boolean and list (array of other
    ///     data types)
    /// </summary>
    private static object ConvertToMixpanelDataType(object value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToIso8601();
        }

        if (value is not string
            && value is IEnumerable array)
        {
            return array.Cast<object>()
                .Select(ConvertToMixpanelDataType)
                .ToList();
        }

        return value;
    }

    private static Dictionary<string, object?> ConvertUserAgentToMixpanelProperties(string userAgent)
    {
        var parser = Parser.GetDefault();
        var info = parser.Parse(userAgent);

        var browser = info.UA.Family;
        var browserVersion = $"{info.UA.Major}.{info.UA.Minor}.{info.UA.Patch}";
        var os = info.OS.Family;
        var device = info.Device.Family;
        if (device.EqualsIgnoreCase("other"))
        {
            device = null;
        }

        var properties = new Dictionary<string, object?>();
        if (browser.HasValue())
        {
            properties.Add("$browser", browser);
        }

        if (browserVersion.HasValue())
        {
            properties.Add("$browser_version", browserVersion);
        }

        if (os.HasValue())
        {
            properties.Add("$os", os);
        }

        if (device.HasValue())
        {
            properties.Add("$device", device);
        }

        return properties;
    }
}