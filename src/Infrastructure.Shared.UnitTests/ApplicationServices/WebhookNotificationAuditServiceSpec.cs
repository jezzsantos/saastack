using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;
using WebhookNotificationAudit = Application.Persistence.Shared.ReadModels.WebhookNotificationAudit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class WebhookNotificationAuditServiceSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IWebhookNotificationAuditRepository> _repository;
    private readonly WebhookNotificationAuditService _service;

    public WebhookNotificationAuditServiceSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _repository = new Mock<IWebhookNotificationAuditRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<WebhookNotificationAudit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookNotificationAudit audit, CancellationToken _) => audit);

        _service = new WebhookNotificationAuditService(recorder.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenCreateAuditAsync_ThenCreates()
    {
        var result = await _service.CreateAuditAsync(_caller.Object, "asource", "aneventid", "aneventtype",
            "ajsoncontent", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.Source.Should().Be("asource");
        result.Value.EventId.Should().Be("aneventid");
        result.Value.EventType.Should().Be("aneventtype");
        result.Value.Status.Should().Be(WebhookNotificationStatus.Received);
        result.Value.JsonContent.Should().Be("ajsoncontent");
        _repository.Verify(rep => rep.SaveAsync(It.Is<WebhookNotificationAudit>(audit =>
            audit.Id.HasValue
            && audit.Source == "asource"
            && audit.EventId == "aneventid"
            && audit.EventType == "aneventtype"
            && audit.Status == WebhookNotificationStatus.Received
            && audit.JsonContent == "ajsoncontent"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenMarkAsFailedProcessingAsync_ThenUpdates()
    {
        var audit = new WebhookNotificationAudit
        {
            Id = "anid",
            Source = "asource",
            EventId = "aneventid",
            EventType = "aneventtype",
            Status = WebhookNotificationStatus.Received,
            JsonContent = "ajsoncontent"
        };
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(audit);

        var result = await _service.MarkAsFailedProcessingAsync(_caller.Object, "anauditid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.Source.Should().Be("asource");
        result.Value.EventId.Should().Be("aneventid");
        result.Value.EventType.Should().Be("aneventtype");
        result.Value.Status.Should().Be(WebhookNotificationStatus.Failed);
        result.Value.JsonContent.Should().Be("ajsoncontent");
        _repository.Verify(rep => rep.SaveAsync(It.Is<WebhookNotificationAudit>(aud =>
            aud.Id.HasValue
            && aud.Source == "asource"
            && aud.EventId == "aneventid"
            && aud.EventType == "aneventtype"
            && aud.Status == WebhookNotificationStatus.Failed
            && aud.JsonContent == "ajsoncontent"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenMarkAsProcessedAsync_ThenUpdates()
    {
        var audit = new WebhookNotificationAudit
        {
            Id = "anid",
            Source = "asource",
            EventId = "aneventid",
            EventType = "aneventtype",
            Status = WebhookNotificationStatus.Received,
            JsonContent = "ajsoncontent"
        };
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(audit);

        var result = await _service.MarkAsProcessedAsync(_caller.Object, "anauditid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.Source.Should().Be("asource");
        result.Value.EventId.Should().Be("aneventid");
        result.Value.EventType.Should().Be("aneventtype");
        result.Value.Status.Should().Be(WebhookNotificationStatus.Processed);
        result.Value.JsonContent.Should().Be("ajsoncontent");
        _repository.Verify(rep => rep.SaveAsync(It.Is<WebhookNotificationAudit>(aud =>
            aud.Id.HasValue
            && aud.Source == "asource"
            && aud.EventId == "aneventid"
            && aud.EventType == "aneventtype"
            && aud.Status == WebhookNotificationStatus.Processed
            && aud.JsonContent == "ajsoncontent"
        ), It.IsAny<CancellationToken>()));
    }
}