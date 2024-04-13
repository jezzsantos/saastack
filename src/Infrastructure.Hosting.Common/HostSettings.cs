using Application.Interfaces.Services;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Images;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides settings for any API host
/// </summary>
public class HostSettings : IHostSettings
{
    internal const string AncillaryApiHmacSecretSettingName = "Hosts:AncillaryApi:HMACAuthNSecret";
    internal const string AncillaryApiHostBaseUrlSettingName = "Hosts:AncillaryApi:BaseUrl";
    internal const string AnyApiBaseUrlSettingName = "Hosts:AnyApi:BaseUrl";
    internal const string ImagesApiHostBaseUrlSettingName = "Hosts:ImagesApi:BaseUrl";
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

    public string MakeImagesApiGetUrl(string imageId)
    {
        var baseUrl = GetImagesApiHostBaseUrl().WithoutTrailingSlash();
        var requestUrl = new DownloadImageRequest { Id = imageId }
            .GetRequestInfo().Route
            .WithoutLeadingSlash();

        return new Uri(new Uri(baseUrl), requestUrl).AbsoluteUri;
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

    private string GetImagesApiHostBaseUrl()
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