using Common.Configuration;
using FluentAssertions;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class WebApiHostSettingsSpec
{
    private readonly WebApiHostSettings _service;
    private readonly Mock<IConfigurationSettings> _settings;

    public WebApiHostSettingsSpec()
    {
        _settings = new Mock<IConfigurationSettings>();
        _service = new WebApiHostSettings(_settings.Object);
    }

    [Fact]
    public void WhenGetWebHostBaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(WebApiHostSettings.WebsiteHostBaseUrlSettingName))
            .Returns("http://localhost/api/");

        var result = _service.GetWebsiteHostBaseUrl();

        result.Should().Be("http://localhost/api");
    }

    [Fact]
    public void WhenGetAncillaryApiHostBaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(WebApiHostSettings.AncillaryApiHostBaseUrlSettingName))
            .Returns("http://localhost/api/");

        var result = _service.GetAncillaryApiHostBaseUrl();

        result.Should().Be("http://localhost/api");
    }
}