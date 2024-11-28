using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace AncillaryApplication.UnitTests;

[Trait("Category", "Unit")]
public class TwilioApplicationSpec
{
    private readonly Mock<IAncillaryApplication> _ancillaryApplication;
    private readonly TwilioApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IWebhookNotificationAuditService> _webhookNotificationAuditService;

    public TwilioApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _ancillaryApplication = new Mock<IAncillaryApplication>();
        _webhookNotificationAuditService = new Mock<IWebhookNotificationAuditService>();
        _webhookNotificationAuditService.Setup(wns => wns.CreateAuditAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookNotificationAudit
            {
                Id = "anauditid",
                Source = "asource",
                EventId = "aneventid",
                EventType = "aneventtype",
                Status = WebhookNotificationStatus.Received
            });
        _webhookNotificationAuditService.Setup(wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookNotificationAudit
            {
                Id = "anauditid",
                Source = "asource",
                EventId = "aneventid",
                EventType = "aneventtype",
                Status = WebhookNotificationStatus.Processed
            });

        _application = new TwilioApplication(recorder.Object, _ancillaryApplication.Object,
            _webhookNotificationAuditService.Object);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithUnhandledEvent_ThenReturnsOk()
    {
        var eventData = new TwilioEventData
        {
            MessageStatus = TwilioMessageStatus.Queued,
            MessageSid = "amessagesid",
            ErrorCode = null,
            RawDlrDoneDate = null
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            TwilioConstants.AuditSourceName, It.Is<string>(s => s.StartsWith("amessagesid")),
            TwilioMessageStatus.Queued.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithDeliveredEvent_ThenConfirms()
    {
        var eventData = new TwilioEventData
        {
            MessageStatus = TwilioMessageStatus.Delivered,
            MessageSid = "amessagesid",
            ErrorCode = null,
            RawDlrDoneDate = 2411271234
        };
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(_caller.Object, "areceiptid",
                new DateTime(2024, 11, 27, 12, 34, 00, DateTimeKind.Utc), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            TwilioConstants.AuditSourceName,
            It.Is<string>(s => s.StartsWith("amessagesid")), TwilioMessageStatus.Delivered.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithFailedEventButNoErrorCode_ThenReturnsOk()
    {
        var eventData = new TwilioEventData
        {
            MessageStatus = TwilioMessageStatus.Failed,
            MessageSid = "amessagesid",
            ErrorCode = null,
            RawDlrDoneDate = null
        };

        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveryFailedAsync(_caller.Object,
                It.Is<string>(s => s.StartsWith("amessagesid")),
                DateTime.UnixEpoch.AddSeconds(1), "none", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            TwilioConstants.AuditSourceName,
            It.Is<string>(s => s.StartsWith("amessagesid")), TwilioMessageStatus.Failed.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithFailedEventWithErrorCode_ThenConfirms()
    {
        var eventData = new TwilioEventData
        {
            MessageStatus = TwilioMessageStatus.Failed,
            MessageSid = "amessagesid",
            ErrorCode = "anerrorcode",
            RawDlrDoneDate = null
        };
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveryFailedAsync(_caller.Object, "areceiptid",
                DateTime.UnixEpoch.AddSeconds(1), "anerrorcode", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            TwilioConstants.AuditSourceName,
            It.Is<string>(s => s.StartsWith("amessagesid")), TwilioMessageStatus.Failed.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}