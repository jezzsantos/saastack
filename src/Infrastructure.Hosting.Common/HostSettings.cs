using Application.Interfaces.Services;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides settings for any API host
/// </summary>
public class HostSettings : IHostSettings
{
    internal const string AncillaryApiHmacSecretSettingName = "Hosts:AncillaryApi:HmacAuthNSecret";
    internal const string AncillaryApiHostBaseUrlSettingName = "Hosts:AncillaryApi:BaseUrl";
    internal const string WebsiteHostBaseUrlSettingName = "Hosts:WebsiteHost:BaseUrl";

    private readonly IConfigurationSettings _settings;

    public HostSettings(IConfigurationSettings settings)
    {
        _settings = settings;
    }

    public string GetAncillaryApiHostBaseUrl()
    {
        var baseUrl = _settings.Platform.GetString(AncillaryApiHostBaseUrlSettingName);
        if (baseUrl.HasValue())
        {
            return baseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(AncillaryApiHostBaseUrlSettingName));
    }

    public string GetWebsiteHostBaseUrl()
    {
        var baseUrl = _settings.Platform.GetString(WebsiteHostBaseUrlSettingName);
        if (baseUrl.HasValue())
        {
            return baseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(WebsiteHostBaseUrlSettingName));
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
}