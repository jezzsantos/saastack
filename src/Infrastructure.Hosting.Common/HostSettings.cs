using Application.Interfaces.Services;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Images;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides settings for the various API hosts
/// </summary>
public class HostSettings : IHostSettings
{
    internal const string AncillaryApiHmacSecretSettingName = "Hosts:AncillaryApi:HMACAuthNSecret";
    internal const string AncillaryApiHostBaseUrlSettingName = "Hosts:AncillaryApi:BaseUrl";
    internal const string ApiHost1BaseUrlSettingName = "Hosts:ApiHost1:BaseUrl";
    internal const string EventNotificationApiHmacSecretSettingName = "Hosts:{0}:HMACAuthNSecret";
    internal const string EventNotificationApiHostBaseUrlSettingName = "Hosts:{0}:BaseUrl";
    internal const string EventNotificationSubscriberSettingName = "Hosts:EventNotificationApi:SubscribedHosts";
    internal const string ImagesApiHostBaseUrlSettingName = "Hosts:ImagesApi:BaseUrl";
    internal const string PrivateInterHostHmacSecretSettingName = "Hosts:PrivateInterHost:HMACAuthNSecret";
    internal const string WebsiteHostBaseUrlSettingName = "Hosts:WebsiteHost:BaseUrl";
    internal const string WebsiteHostCSRFEncryptionSettingName = "Hosts:WebsiteHost:CSRFAESSecret";
    internal const string WebsiteHostCSRFSigningSettingName = "Hosts:WebsiteHost:CSRFHMACSecret";
    private static readonly char[] SubscribedHostIdSeparators = [',', ';', ' '];

    private readonly IConfigurationSettings _settings;

    public HostSettings(IConfigurationSettings settings)
    {
        _settings = settings;
    }

    public virtual string GetAncillaryApiHostBaseUrl()
    {
        var baseUrl = _settings.Platform.GetString(AncillaryApiHostBaseUrlSettingName);
        if (baseUrl.HasValue())
        {
            return baseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(AncillaryApiHostBaseUrlSettingName));
    }

    public string GetAncillaryApiHostHmacAuthSecret()
    {
        var secret = _settings.Platform.GetString(AncillaryApiHmacSecretSettingName);
        if (secret.HasValue())
        {
            return secret;
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(AncillaryApiHmacSecretSettingName));
    }

    public virtual string GetApiHost1BaseUrl()
    {
        var baseUrl = _settings.Platform.GetString(ApiHost1BaseUrlSettingName);
        if (baseUrl.HasValue())
        {
            return baseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(ApiHost1BaseUrlSettingName));
    }

    public virtual IReadOnlyList<SubscriberHost> GetEventNotificationSubscriberHosts()
    {
        var ids = _settings.Platform.GetString(EventNotificationSubscriberSettingName, string.Empty);
        if (ids.HasNoValue())
        {
            return [];
        }

        return ids.Split(SubscribedHostIdSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(id =>
            {
                var baseUrl = _settings.Platform.GetString(EventNotificationApiHostBaseUrlSettingName.Format(id));
                var hmacSecret = _settings.Platform.GetString(EventNotificationApiHmacSecretSettingName.Format(id));
                return new SubscriberHost(id, baseUrl, hmacSecret);
            }).ToList();
    }

    public string GetPrivateInterHostHmacAuthSecret()
    {
        var secret = _settings.Platform.GetString(PrivateInterHostHmacSecretSettingName);
        if (secret.HasValue())
        {
            return secret;
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(PrivateInterHostHmacSecretSettingName));
    }

    public virtual string GetWebsiteHostBaseUrl()
    {
        var baseUrl = _settings.Platform.GetString(WebsiteHostBaseUrlSettingName);
        if (baseUrl.HasValue())
        {
            return baseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(WebsiteHostBaseUrlSettingName));
    }

    public string GetWebsiteHostCSRFEncryptionSecret()
    {
        var secret = _settings.Platform.GetString(WebsiteHostCSRFEncryptionSettingName);
        if (secret.HasValue())
        {
            return secret;
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(WebsiteHostCSRFEncryptionSettingName));
    }

    public string GetWebsiteHostCSRFSigningSecret()
    {
        var secret = _settings.Platform.GetString(WebsiteHostCSRFSigningSettingName);
        if (secret.HasValue())
        {
            return secret;
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(WebsiteHostCSRFSigningSettingName));
    }

    public string MakeImagesApiGetUrl(string imageId)
    {
        var baseUrl = GetImagesApiHostBaseUrl().WithoutTrailingSlash();
        var requestUrl = new DownloadImageRequest { Id = imageId }
            .GetRequestInfo().Route
            .WithoutLeadingSlash();

        return new Uri(new Uri(baseUrl), requestUrl).AbsoluteUri;
    }

    protected virtual string GetImagesApiHostBaseUrl()
    {
        var apiHostBaseUrl = _settings.Platform.GetString(ImagesApiHostBaseUrlSettingName);
        if (apiHostBaseUrl.HasValue())
        {
            return apiHostBaseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(ImagesApiHostBaseUrlSettingName));
    }
}