using Application.Interfaces;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Defines a translator that translates usage delivery events.
/// </summary>
public interface IUsageDeliveryTranslator
{
    /// <summary>
    ///     Returns the user's ID
    /// </summary>
    public string UserId { get; }

    /// <summary>
    ///     Returns the browser properties from the additional data
    /// </summary>
    BrowserComponents GetBrowserProperties(Dictionary<string, string> additional);

    /// <summary>
    ///     Converts a user agent string into individual properties
    /// </summary>
    UserAgentComponents GetUserAgentProperties(string? userAgent);

    /// <summary>
    ///     Returns additional user properties based on the event
    /// </summary>
    UserComponents GetUserProperties(string eventName, Dictionary<string, string>? additional);

    /// <summary>
    ///     Determines whether the specified event identifies the user, and if so returns their ID.
    /// </summary>
    bool IsUserIdentifiableEvent();

    /// <summary>
    ///     Prepares the properties of the event for delivery.
    ///     This method applies any additional properties, converting value with the specified <see cref="converter" />.
    ///     This method removes any properties that are not relevant for the event.
    /// </summary>
    Dictionary<string, string> PrepareProperties(bool overrideOrganizationId, Func<string, string> converter);

    /// <summary>
    ///     Recalculates the tenant ID and user ID and overrides based on the specified <paramref name="tenantId" />.
    /// </summary>
    void RecalculateTenantId(string tenantId);

    /// <summary>
    ///     Begins the translation process for a usage delivery event.
    ///     Note: must be called before any other methods.
    /// </summary>
    void StartTranslation(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional, bool createdTenantedUserIds);
}

public class UserAgentComponents
{
    public string? Browser { get; set; }

    public string? BrowserVersion { get; set; }

    public string? Device { get; set; }

    public string? OperatingSystem { get; set; }
}

public class BrowserComponents
{
    public string? IpAddress { get; set; }

    public string? Referrer { get; set; }

    public string? Url { get; set; }
}

public class UserComponents
{
    public string? AvatarUrl { get; set; }

    public string? CountryCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? EmailAddress { get; set; }

    public string? Name { get; set; }

    public string? Timezone { get; set; }
}