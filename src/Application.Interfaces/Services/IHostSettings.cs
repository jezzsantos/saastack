using System.Net.Mime;
using Common;

namespace Application.Interfaces.Services;

/// <summary>
///     Defines settings for the current API host
/// </summary>
public interface IHostSettings
{
    /// <summary>
    ///     Returns the URL for the Ancillary API host
    /// </summary>
    string GetAncillaryApiHostBaseUrl();

    /// <summary>
    ///     Returns the HMAC auth secret for the Ancillary API Host
    /// </summary>
    string GetAncillaryApiHostHmacAuthSecret();

    /// <summary>
    ///     Returns the URL of the ApiHost1
    /// </summary>
    /// <returns></returns>
    string GetApiHost1BaseUrl();

    /// <summary>
    ///     Returns the subscribing hosts for event notifications
    /// </summary>
    IReadOnlyList<SubscriberHost> GetEventNotificationSubscriberHosts();

    /// <summary>
    ///     Returns the HMAC auth secret for Inter Host
    /// </summary>
    string GetPrivateInterHostHmacAuthSecret();

    /// <summary>
    ///     Returns the region that this host is running in
    /// </summary>
    public DatacenterLocation GetRegion();

    /// <summary>
    ///     Returns the URL of the Website host
    /// </summary>
    string GetWebsiteHostBaseUrl();

    /// <summary>
    ///     Returns the CSRF encryption secret
    /// </summary>
    string GetWebsiteHostCSRFEncryptionSecret();

    /// <summary>
    ///     Returns the CSRF signature secret
    /// </summary>
    string GetWebsiteHostCSRFSigningSecret();

    /// <summary>
    ///     Returns the URL for the specified <see cref="MediaTypeNames.Image" />
    /// </summary>
    string MakeImagesApiGetUrl(string imageId);
}

/// <summary>
///     Defines an event notification subscriber host
/// </summary>
public record SubscriberHost(string HostName, string BaseUrl, string HmacSecret);