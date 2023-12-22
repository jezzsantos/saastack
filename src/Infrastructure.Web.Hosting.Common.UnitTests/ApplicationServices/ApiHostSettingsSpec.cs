using Common.Configuration;
using FluentAssertions;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ApiHostSettingsSpec
{
    private readonly ApiHostSettings _service;
    private readonly Mock<IConfigurationSettings> _settings;

    public ApiHostSettingsSpec()
    {
        _settings = new Mock<IConfigurationSettings>();
        _service = new ApiHostSettings(_settings.Object);
    }

    [Fact]
    public void WhenGetWebsiteHostBaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(ApiHostSettings.WebsiteHostBaseUrlSettingName))
            .Returns("http://localhost/api/");

        var result = _service.GetWebsiteHostBaseUrl();

        result.Should().Be("http://localhost/api");
    }

    [Fact]
    public void WhenGetAncillaryApiHostBaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(ApiHostSettings.AncillaryApiHostBaseUrlSettingName))
            .Returns("http://localhost/api/");

        var result = _service.GetAncillaryApiHostBaseUrl();

        result.Should().Be("http://localhost/api");
    }

    [Fact]
    public void WhenGetAncillaryApiHostHmacAuthSecret_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(ApiHostSettings.AncillaryApiHmacSecretSettingName))
            .Returns("asecret");

        var result = _service.GetAncillaryApiHostHmacAuthSecret();

        result.Should().Be("asecret");
    }
}