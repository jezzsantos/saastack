using AncillaryApplication;
using AncillaryInfrastructure.Api._3rdParties;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using FluentAssertions;
using Infrastructure.External.ApplicationServices;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;
using Microsoft.AspNetCore.Http;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api._3rdParties;

[Trait("Category", "Unit")]
public class MailgunApiSpec
{
    private readonly MailgunApi _api;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IMailgunApplication> _mailgunApplication;
    private readonly Mock<IRecorder> _recorder;
    private readonly MailgunSignature _signature;

    public MailgunApiSpec()
    {
        _recorder = new Mock<IRecorder>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request = { IsHttps = true }
            });
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallId).Returns("acallid");
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(cf => cf.Create())
            .Returns(_caller.Object);
        _mailgunApplication = new Mock<IMailgunApplication>();
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s =>
                s.Platform.GetString(MailgunClient.Constants.WebhookSigningKeySettingName, It.IsAny<string>()))
            .Returns("asigningkey");
        _signature = new MailgunSignature
        {
            Timestamp = "1",
            Token = "atoken",
            Signature = "94459ca3fba955a4b74979442712cceb37d2e64aab2d86809213dbfd40123c70"
        };

        _api = new MailgunApi(_recorder.Object, _httpContextAccessor.Object, callerFactory.Object,
            settings.Object, _mailgunApplication.Object);
    }

    [Fact]
    public void WhenAuthenticateRequestAndNotHttps_ThenReturnsError()
    {
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request = { IsHttps = false }
            });

        var result = MailgunApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            _signature, "asigningkey");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMissingSigningKey_ThenReturnsError()
    {
        var result = MailgunApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            _signature, "");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMissingSignature_ThenReturnsError()
    {
        var result = MailgunApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            new MailgunSignature(), "asigningkey");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMismatchedSignature_ThenReturnsError()
    {
        var result = MailgunApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            new MailgunSignature
            {
                Timestamp = "1",
                Signature = "aninvalidsignature",
                Token = "atoken"
            }
            , "asigningkey");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndValidSignatureMatchingKey_ThenReturnsOk()
    {
        var result = MailgunApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            _signature
            , "asigningkey");

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenNotifyWebhookEventAndWithMissingEvent_ThenReturnsEmptyResponse()
    {
        var result = await _api.NotifyWebhookEvent(new MailgunNotifyWebhookEventRequest
        {
            Signature = _signature,
            EventData = new MailgunEventData
            {
                Message = new MailgunMessage()
            }
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _mailgunApplication.Verify(app => app.NotifyWebhookEvent(It.IsAny<ICallerContext>(),
            It.IsAny<MailgunEventData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEvent_ThenNotifies()
    {
        _mailgunApplication.Setup(app => app.NotifyWebhookEvent(It.IsAny<ICallerContext>(),
                It.IsAny<MailgunEventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var eventData = new MailgunEventData
        {
            Event = "anevent"
        };

        var result = await _api.NotifyWebhookEvent(new MailgunNotifyWebhookEventRequest
        {
            Signature = _signature,
            EventData = eventData
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _mailgunApplication.Verify(app => app.NotifyWebhookEvent(It.Is<ICallerContext>(cc => cc.CallId == "acallid"),
            eventData, It.IsAny<CancellationToken>()));
    }
}