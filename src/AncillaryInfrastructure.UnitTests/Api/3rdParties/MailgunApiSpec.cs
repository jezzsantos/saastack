using AncillaryApplication;
using AncillaryInfrastructure.Api._3rdParties;
using Application.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api._3rdParties;

[Trait("Category", "Unit")]
public class MailgunApiSpec
{
    private readonly Mock<IAncillaryApplication> _ancillaryApplication;
    private readonly MailgunApi _api;
    private readonly MailgunSignature _signature;

    public MailgunApiSpec()
    {
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(cf => cf.Create())
            .Returns(Mock.Of<ICallerContext>());
        _ancillaryApplication = new Mock<IAncillaryApplication>();
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s => s.Platform.GetString(MailgunConstants.WebhookSigningKeySettingName, It.IsAny<string>()))
            .Returns("asecret");
        _signature = new MailgunSignature
        {
            Timestamp = "1",
            Token = "atoken",
            Signature = "bf106940253fa7477ba4b55a027126b70037ce9b00e67aa3bf4f5bab2775d3e1"
        };

        _api = new MailgunApi(callerFactory.Object, _ancillaryApplication.Object, settings.Object);
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndInvalidSignature_ThenReturnsError()
    {
        var result = await _api.NotifyEmailDeliveryReceipt(new MailgunNotifyWebhookEventRequest
        {
            Signature = new MailgunSignature()
        }, CancellationToken.None);

        result().Should().BeError(ErrorCode.NotAuthenticated);
        _ancillaryApplication.Verify(app => app.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndUnhandledEvent_ThenReturnsEmptyResponse()
    {
        var result = await _api.NotifyEmailDeliveryReceipt(new MailgunNotifyWebhookEventRequest
        {
            Signature = _signature,
            EventData = new MailgunEventData
            {
                Event = "anunknownevent"
            }
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _ancillaryApplication.Verify(app => app.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndWithNoReceiptId_ThenReturnsEmptyResponse()
    {
        var result = await _api.NotifyEmailDeliveryReceipt(new MailgunNotifyWebhookEventRequest
        {
            Signature = _signature,
            EventData = new MailgunEventData
            {
                Event = MailgunConstants.Events.Delivered,
                Message = new MailgunMessage()
            }
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _ancillaryApplication.Verify(app => app.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndWithDeliveredEvent_ThenReturnsEmptyResponse()
    {
        _ancillaryApplication.Setup(app => app.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var deliveredAt = DateTime.UtcNow.ToNearestSecond().ToUnixSeconds();
        var result = await _api.NotifyEmailDeliveryReceipt(new MailgunNotifyWebhookEventRequest
        {
            Signature = _signature,
            EventData = new MailgunEventData
            {
                Event = MailgunConstants.Events.Delivered,
                Message = new MailgunMessage
                {
                    Headers = new MailgunMessageHeaders
                    {
                        MessageId = "areceiptid"
                    }
                },
                Timestamp = deliveredAt
            }
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _ancillaryApplication.Verify(app => app.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(),
            "areceiptid", deliveredAt.FromUnixTimestamp(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndWithFailedEvent_ThenReturnsEmptyResponse()
    {
        _ancillaryApplication.Setup(app => app.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var failedAt = DateTime.UtcNow.ToNearestSecond().ToUnixSeconds();
        var result = await _api.NotifyEmailDeliveryReceipt(new MailgunNotifyWebhookEventRequest
        {
            Signature = _signature,
            EventData = new MailgunEventData
            {
                Event = MailgunConstants.Events.Failed,
                Message = new MailgunMessage
                {
                    Headers = new MailgunMessageHeaders
                    {
                        MessageId = "areceiptid"
                    }
                },
                Timestamp = failedAt,
                Severity = MailgunConstants.Values.PermanentSeverity,
                Reason = "areason"
            }
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _ancillaryApplication.Verify(app => app.ConfirmEmailDeliveryFailedAsync(It.IsAny<ICallerContext>(),
            "areceiptid", failedAt.FromUnixTimestamp(), "areason", It.IsAny<CancellationToken>()));
    }
}