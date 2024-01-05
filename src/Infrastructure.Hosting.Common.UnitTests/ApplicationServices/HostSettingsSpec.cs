using Common.Configuration;
using FluentAssertions;
using Moq;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class HostSettingsSpec
{
    private readonly HostSettings _service;
    private readonly Mock<IConfigurationSettings> _settings;

    public HostSettingsSpec()
    {
        _settings = new Mock<IConfigurationSettings>();
        _service = new HostSettings(_settings.Object);
    }

    [Fact]
    public void WhenGetWebsiteHostBaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.WebsiteHostBaseUrlSettingName, It.IsAny<string>()))
            .Returns("http://localhost/api/");

        var result = _service.GetWebsiteHostBaseUrl();

        result.Should().Be("http://localhost/api");
    }

    [Fact]
    public void WhenGetAncillaryApiHostBaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.AncillaryApiHostBaseUrlSettingName, It.IsAny<string>()))
            .Returns("http://localhost/api/");

        var result = _service.GetAncillaryApiHostBaseUrl();

        result.Should().Be("http://localhost/api");
    }

    [Fact]
    public void WhenGetAncillaryApiHostHmacAuthSecret_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.AncillaryApiHmacSecretSettingName, It.IsAny<string>()))
            .Returns("asecret");

        var result = _service.GetAncillaryApiHostHmacAuthSecret();

        result.Should().Be("asecret");
    }
}