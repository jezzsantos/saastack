using Common.Configuration;
using Common.Extensions;
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
    public void WhenGetAncillaryApiHostHmacAuthSecret_ThenReturnsSecret()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.AncillaryApiHmacSecretSettingName, It.IsAny<string>()))
            .Returns("asecret");

        var result = _service.GetAncillaryApiHostHmacAuthSecret();

        result.Should().Be("asecret");
    }

    [Fact]
    public void WhenGetApiHost1BaseUrl_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.ApiHost1BaseUrlSettingName, It.IsAny<string>()))
            .Returns("http://localhost/api/");

        var result = _service.GetApiHost1BaseUrl();

        result.Should().Be("http://localhost/api");
    }

    [Fact]
    public void WhenGetWebsiteHostCSRFSigningSecret_ThenReturnsBaseUrl()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.WebsiteHostCSRFSigningSettingName, It.IsAny<string>()))
            .Returns("asecret");

        var result = _service.GetWebsiteHostCSRFSigningSecret();

        result.Should().Be("asecret");
    }

    [Fact]
    public void WhenGetWebsiteHostCSRFEncryptionSecret_ThenReturnsBaseUrl()
    {
        _settings.Setup(
                s => s.Platform.GetString(HostSettings.WebsiteHostCSRFEncryptionSettingName, It.IsAny<string>()))
            .Returns("asecret");

        var result = _service.GetWebsiteHostCSRFEncryptionSecret();

        result.Should().Be("asecret");
    }

    [Fact]
    public void WhenMakeImagesApiGetUrl_ThenReturnsUrl()
    {
        _settings.Setup(s => s.Platform.GetString(HostSettings.ImagesApiHostBaseUrlSettingName, It.IsAny<string>()))
            .Returns("http://localhost/");

        var result = _service.MakeImagesApiGetUrl("animageid");

        result.Should().Be("http://localhost/images/animageid/download");
    }

    [Fact]
    public void WhenGetEventNotificationSubscribersAndNoSubscribedHosts_ThenReturnsNoSubscribers()
    {
        _settings.Setup(s =>
                s.Platform.GetString(HostSettings.EventNotificationSubscriberSettingName,
                    It.IsAny<string>()))
            .Returns(string.Empty);

        var result = _service.GetEventNotificationSubscriberHosts();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetEventNotificationSubscribersAndASubscribedHost_ThenReturnsSubscribers()
    {
        _settings.Setup(s =>
                s.Platform.GetString(HostSettings.EventNotificationSubscriberSettingName,
                    It.IsAny<string>()))
            .Returns("asubscribedhost");
        _settings.Setup(s =>
                s.Platform.GetString(HostSettings.EventNotificationApiHostBaseUrlSettingName.Format("asubscribedhost"),
                    It.IsAny<string>()))
            .Returns("http://localhost/api");
        _settings.Setup(s =>
                s.Platform.GetString(HostSettings.EventNotificationApiHmacSecretSettingName.Format("asubscribedhost"),
                    It.IsAny<string>()))
            .Returns("asecret");

        var result = _service.GetEventNotificationSubscriberHosts();

        result.Should().OnlyContain(x =>
            x.HostName == "asubscribedhost" && x.BaseUrl == "http://localhost/api" && x.HmacSecret == "asecret");
    }

    [Fact]
    public void WhenGetPrivateInterHostHmacAuthSecret_ThenReturnsSecret()
    {
        _settings.Setup(s =>
                s.Platform.GetString(HostSettings.PrivateInterHostHmacSecretSettingName, It.IsAny<string>()))
            .Returns("asecret");

        var result = _service.GetPrivateInterHostHmacAuthSecret();

        result.Should().Be("asecret");
    }
}