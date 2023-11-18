using Application.Interfaces.Services;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Web.Hosting.Common;

public class WebApiHostSettings : IApiHostSetting
{
    internal const string AncillaryApiHostBaseUrlSettingName = "Hosts:AncillaryApi:BaseUrl";
    internal const string WebsiteHostBaseUrlSettingName = "Hosts:WebsiteHost:BaseUrl";

    private readonly IConfigurationSettings _settings;

    public WebApiHostSettings(IConfigurationSettings settings)
    {
        _settings = settings;
    }

    public string GetAncillaryApiHostBaseUrl()
    {
        var apiHostBaseUrl = _settings.Platform.GetString(AncillaryApiHostBaseUrlSettingName);
        if (apiHostBaseUrl.HasValue())
        {
            return apiHostBaseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.WebApiHostSettings_ApiHostBaseUrlMissing.Format(AncillaryApiHostBaseUrlSettingName));
    }

    public string GetWebsiteHostBaseUrl()
    {
        var webHostBaseUrl = _settings.Platform.GetString(WebsiteHostBaseUrlSettingName);
        if (webHostBaseUrl.HasValue())
        {
            return webHostBaseUrl.WithoutTrailingSlash();
        }

        throw new InvalidOperationException(
            Resources.WebApiHostSettings_WebHostBaseUrlMissing.Format(WebsiteHostBaseUrlSettingName));
    }
}