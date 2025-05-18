using System.Collections;
using System.Text.Json;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides an adapter to the Mixpanel.com service
///     <see href="https://developer.mixpanel.com/reference/overview" />
///     Note: In Mixpanel, users are unique across all companies.
///     Thus, when we identify/profile a user, we use their userId as their distinct_id.
///     Anonymous users should have an empty distinct_id.
///     Mixpanel defines numerous supported reserved properties for events.
///     Events will be treated as duplicates if they share the same $insert_id + distinct_id + time + eventName.
/// </summary>
public class MixpanelHttpServiceClient : IUsageDeliveryService
{
    private static readonly string[]
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
        UsageDeliveryTranslator.BrowserReferrer, UsageConstants.Properties.ReferredBy,
        UsageConstants.Properties.Path,
        UsageDeliveryTranslator.BrowserIp, UsageConstants.Properties.IpAddress,
        UsageConstants.Properties.UserAgent,
        MixpanelConstants.MetadataProperties.DistinctId,
        MixpanelConstants.MetadataProperties.Time,
        MixpanelConstants.MetadataProperties.ReferredBy,
        MixpanelConstants.MetadataProperties.IpAddress,
        MixpanelConstants.MetadataProperties.Url
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
        _translator.StartTranslation(caller, forId, eventName, additional, false);
        var userId = _translator.UserId;
        var isIdentifiableEvent = _translator.IsUserIdentifiableEvent();
        if (isIdentifiableEvent)
        {
            var identified = await SetProfileAsync(caller, userId, eventName, additional, cancellationToken);
            if (identified.IsFailure)
            {
                return identified.Error;
            }
        }

        var eventProperties =
            _translator.PrepareProperties(true, value => ConvertToMixpanelDataType(value).ToString()!);
        var imported = await ImportEventAsync(caller, userId, eventName, eventProperties, cancellationToken);
        if (imported.IsFailure)
        {
            return imported;
        }

        return Result.Ok;
    }
#if TESTINGONLY
    public static string TestingOnly_SanitizeDistinctId(string distinctId)
    {
        return SanitizeDistinctId(distinctId);
    }
#endif

#if TESTINGONLY
    public static string TestingOnly_SanitizeInsertId(string insertId)
    {
        return SanitizeInsertId(insertId);
    }
#endif

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

        _recorder.TraceInformation(caller.ToCall(), "Imported event {Event} in Mixpanel for {User} successfully",
            eventName, userId);

        return Result.Ok;
    }

    private async Task<Result<Error>> SetProfileAsync(ICallerContext caller, string userId,
        string eventName, Dictionary<string, string>? additional, CancellationToken cancellationToken)
    {
        var properties = CalculateProfileProperties(eventName, additional);

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

    private MixpanelProfileProperties CalculateProfileProperties(string eventName,
        Dictionary<string, string>? additional)
    {
        var components = _translator.GetUserProperties(eventName, additional);
        var properties = new MixpanelProfileProperties
        {
            Unsubscribed = true,
            Name = components.Name,
            Email = components.EmailAddress,
            Timezone = components.Timezone,
            CountryCode = components.CountryCode,
            Avatar = components.AvatarUrl
        };

        return properties;
    }

    private MixpanelEventProperties CalculateImportProperties(ICallerContext caller,
        string userId, Dictionary<string, string> eventProperties)
    {
        var distinctId = SanitizeDistinctId(userId);
        var insertId = SanitizeInsertId(caller.CallId);
        var time = GetTimestamp(eventProperties);
        var browserProperties = _translator.GetBrowserProperties(eventProperties);
        var properties = new MixpanelEventProperties
        {
            Time = time,
            DistinctId = distinctId,
            InsertId = insertId,
            ReferredBy = browserProperties.Referrer,
            Url = browserProperties.Url,
            Ip = browserProperties.IpAddress
        };
        var userAgent = eventProperties.GetValueOrDefault(UsageConstants.Properties.UserAgent);
        var components = _translator.GetUserAgentProperties(userAgent);
        if (components.Browser.HasValue())
        {
            properties.Add(MixpanelConstants.MetadataProperties.Browser,
                ConvertToMixpanelDataType(components.Browser));
        }

        if (components.BrowserVersion.HasValue())
        {
            properties.Add(MixpanelConstants.MetadataProperties.BrowserVersion,
                ConvertToMixpanelDataType(components.BrowserVersion));
        }

        if (components.OperatingSystem.HasValue())
        {
            properties.Add(MixpanelConstants.MetadataProperties.OperatingSystem,
                ConvertToMixpanelDataType(components.OperatingSystem));
        }

        if (components.Device.HasValue())
        {
            properties.Add(MixpanelConstants.MetadataProperties.Device,
                ConvertToMixpanelDataType(components.Device));
        }

        var tenantId = eventProperties.GetValueOrDefault(UsageConstants.Properties.TenantId);
        properties.Add(MixpanelConstants.MetadataProperties.TenantId, tenantId);
        var callId = eventProperties.GetValueOrDefault(UsageConstants.Properties.CallId) ??
                     caller.CallId;
        properties.Add(MixpanelConstants.MetadataProperties.CallId, callId);

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

        if (distinctId.EqualsIgnoreCase(CallerConstants.AnonymousUserId)
            || distinctId.EqualsIgnoreCase("anonymous")) // UsageTranslationOptions.AnonymousUserId
        {
            return string.Empty;
        }

        return ForbiddenMixpanelDistinctIds.ContainsIgnoreCase(distinctId)
            ? string.Empty
            : distinctId;
    }

    /// <summary>
    ///     Mixpanel requires a value for this that is only alphanumeric plus dashes and less than 36 characters long.
    /// </summary>
    private static string SanitizeInsertId(string insertId)
    {
        return insertId.ReplaceWith(@"[^a-zA-Z0-9-]", string.Empty)
            .Substring(0, Math.Min(insertId.Length, 36));
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
}