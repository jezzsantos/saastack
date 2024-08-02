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
public class MailgunApplicationSpec
{
    private readonly Mock<IAncillaryApplication> _ancillaryApplication;
    private readonly MailgunApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IWebhookNotificationAuditService> _webhookNotificationAuditService;

    public MailgunApplicationSpec()
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

        _application = new MailgunApplication(recorder.Object, _ancillaryApplication.Object,
            _webhookNotificationAuditService.Object);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithUnhandledEvent_ThenReturnsOk()
    {
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Unknown.ToString()
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Unknown.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithDeliveredEventButNoMessageId_ThenReturnsOk()
    {
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Delivered.ToString(),
            Message = new MailgunMessage()
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Verify(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never());
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Delivered.ToString(),
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
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Delivered.ToString(),
            Timestamp = 1,
            Message = new MailgunMessage
            {
                Headers = new MailgunMessageHeaders
                {
                    MessageId = "areceiptid"
                }
            }
        };
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(_caller.Object, "areceiptid",
                DateTime.UnixEpoch.AddSeconds(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Delivered.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithFailedEventButTemporarySeverity_ThenReturnsOk()
    {
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Failed.ToString(),
            Severity = MailgunConstants.Values.TemporarySeverity,
            Message = new MailgunMessage()
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Verify(aa => aa.ConfirmEmailDeliveryFailedAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Failed.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithFailedEventButNoMessageId_ThenReturnsOk()
    {
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Failed.ToString(),
            Severity = MailgunConstants.Values.PermanentSeverity,
            Message = new MailgunMessage()
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Verify(aa => aa.ConfirmEmailDeliveryFailedAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Failed.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithFailedEventWithDetailedReason_ThenConfirms()
    {
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Failed.ToString(),
            Severity = MailgunConstants.Values.PermanentSeverity,
            Timestamp = 1,
            Message = new MailgunMessage
            {
                Headers = new MailgunMessageHeaders
                {
                    MessageId = "areceiptid"
                }
            },
            Reason = null,
            DeliveryStatus = new MailgunDeliveryStatus
            {
                Description = "adetailedreason"
            }
        };
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveryFailedAsync(_caller.Object, "areceiptid",
                DateTime.UnixEpoch.AddSeconds(1), "adetailedreason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Failed.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithFailedEventWithReason_ThenConfirms()
    {
        var eventData = new MailgunEventData
        {
            Id = "aneventid",
            Event = MailgunEventType.Failed.ToString(),
            Severity = MailgunConstants.Values.PermanentSeverity,
            Timestamp = 1,
            Message = new MailgunMessage
            {
                Headers = new MailgunMessageHeaders
                {
                    MessageId = "areceiptid"
                }
            },
            Reason = "areason"
        };
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveredAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.NotifyWebhookEvent(_caller.Object, eventData, CancellationToken.None);

        result.Should().BeSuccess();
        _ancillaryApplication.Setup(aa => aa.ConfirmEmailDeliveryFailedAsync(_caller.Object, "areceiptid",
                DateTime.UnixEpoch.AddSeconds(1), "areason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            MailgunConstants.AuditSourceName,
            "aneventid", MailgunEventType.Failed.ToString(),
            eventData.ToJson(false, StringExtensions.JsonCasing.Pascal, false), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}