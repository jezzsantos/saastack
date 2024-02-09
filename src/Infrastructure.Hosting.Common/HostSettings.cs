using Application.Interfaces.Services;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides settings for any API host
/// </summary>
public class HostSettings : IHostSettings
{
    internal const string AncillaryApiHmacSecretSettingName = "Hosts:AncillaryApi:HMACAuthNSecret";
    internal const string AncillaryApiHostBaseUrlSettingName = "Hosts:AncillaryApi:BaseUrl";
    internal const string AnyApiBaseUrlSettingName = "Hosts:AnyApi:BaseUrl";
    internal const string WebsiteHostBaseUrlSettingName = "Hosts:WebsiteHost:BaseUrl";
    internal const string WebsiteHostCSRFEncryptionSettingName = "Hosts:WebsiteHost:CSRFAESSecret";
    internal const string WebsiteHostCSRFSigningSettingName = "Hosts:WebsiteHost:CSRFHMACSecret";

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

    public string GetApiHost1BaseUrl()
    {
        var baseUrl = _settings.Platform.GetString(AnyApiBaseUrlSettingName);
        if (baseUrl.HasValue())
        {
            return baseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.HostSettings_MissingSetting.Format(AnyApiBaseUrlSettingName));
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
}