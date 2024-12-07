using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryApplication.UnitTests;

[Trait("Category", "Unit")]
public class AncillaryApplicationEmailingSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEmailDeliveryRepository> _emailDeliveryRepository;
    private readonly Mock<IEmailDeliveryService> _emailDeliveryService;
    private readonly Mock<IEmailMessageQueue> _emailMessageQueue;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

    public AncillaryApplicationEmailingSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        var usageMessageQueue = new Mock<IUsageMessageQueue>();
        var usageDeliveryService = new Mock<IUsageDeliveryService>();
        var auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        _emailMessageQueue = new Mock<IEmailMessageQueue>();
        _emailDeliveryService = new Mock<IEmailDeliveryService>();
        _emailDeliveryService.Setup(eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryReceipt());
        _emailDeliveryRepository = new Mock<IEmailDeliveryRepository>();
        _emailDeliveryRepository.Setup(ar =>
                ar.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailDeliveryRoot root, bool _, CancellationToken _) => root);
        _emailDeliveryRepository.Setup(edr =>
                edr.FindByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EmailDeliveryRoot>.None);
        var smsMessageQueue = new Mock<ISmsMessageQueue>();
        var smsDeliveryService = new Mock<ISmsDeliveryService>();
        var smsDeliveryRepository = new Mock<ISmsDeliveryRepository>();
        var provisioningMessageQueue = new Mock<IProvisioningMessageQueue>();
        var provisioningDeliveryService = new Mock<IProvisioningNotificationService>();

        _application = new AncillaryApplication(_recorder.Object, _idFactory.Object, usageMessageQueue.Object,
            usageDeliveryService.Object, auditMessageRepository.Object, auditRepository.Object,
            _emailMessageQueue.Object, _emailDeliveryService.Object, _emailDeliveryRepository.Object,
            smsMessageQueue.Object, smsDeliveryService.Object, smsDeliveryRepository.Object,
            provisioningMessageQueue.Object, provisioningDeliveryService.Object);
    }


    [Fact]
    public async Task WhenSendEmailAsyncAndMessageHasNoHtmlNorTemplate_ThenReturnsError()
    {
        var messageAsJson = new EmailMessage
        {
            Html = null,
            Template = null
        }.ToJson()!;

        var result = await _application.SendEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AncillaryApplication_Email_MissingMessage);
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenSendEmailAsyncWithHtmlMessage_ThenSends()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                Body = "abody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender",
                Tags = ["atag"]
            }
        }.ToJson()!;
        var email = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        email.SetContent("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value,
            new List<string> { "atag" });
        email.AttemptSending();
        email.SucceededSending("areceiptid");
        _emailDeliveryRepository.Setup(edr =>
                edr.FindByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email.ToOptional());
        _emailDeliveryService.Setup(eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryReceipt());

        var result = await _application.SendEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryService.Verify(
            eds => eds.SendTemplatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryRepository.Verify(
            edr => edr.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenSendEmailAsyncWithTemplatedMessage_ThenSends()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = null,
            Template = new QueuedEmailTemplatedMessage
            {
                TemplateId = "atemplateid",
                Subject = "asubject",
                Substitutions = new Dictionary<string, string> { { "aname", "avalue" } },
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender",
                Tags = ["atag"]
            }
        }.ToJson()!;
        var email = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        email.SetContent("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value,
            new List<string> { "atag" });
        email.AttemptSending();
        email.SucceededSending("areceiptid");
        _emailDeliveryRepository.Setup(edr =>
                edr.FindByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email.ToOptional());
        _emailDeliveryService.Setup(eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryReceipt());

        var result = await _application.SendEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryService.Verify(
            eds => eds.SendTemplatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryRepository.Verify(
            edr => edr.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenSendEmailAsyncAndNotDelivered_ThenFailsSending()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                Body = "abody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender",
                Tags = ["atag", "anothertag"]
            }
        }.ToJson()!;
        _emailDeliveryService.Setup(eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected());

        var result = await _application.SendEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected);
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), "asubject", "abody", "arecipient@company.com",
                "arecipient", "asender@company.com", "asender", new List<string> { "atag", "anothertag" },
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            eds => eds.SendTemplatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.EmailAddress == "arecipient@company.com"
            && root.Recipient.Value.DisplayName == "arecipient"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsSent == false
        ), true, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSendEmailAsyncWithHtmlMessageAndAlreadyDelivered_ThenDoesNotResend()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                Body = "abody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender",
                Tags = ["atag", "anothertag"]
            }
        }.ToJson()!;
        _emailDeliveryService.Setup(eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryReceipt());

        var result = await _application.SendEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), "asubject", "abody", "arecipient@company.com",
                "arecipient", "asender@company.com", "asender", new List<string> { "atag", "anothertag" },
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            eds => eds.SendTemplatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.EmailAddress == "arecipient@company.com"
            && root.Recipient.Value.DisplayName == "arecipient"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsSent == true
        ), true, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSendEmailAsyncWithTemplatedMessageAndAlreadyDelivered_ThenDoesNotResend()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Template = new QueuedEmailTemplatedMessage
            {
                TemplateId = "atemplateid",
                Subject = "asubject",
                Substitutions = null,
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender",
                Tags = ["atag", "anothertag"]
            }
        }.ToJson()!;
        _emailDeliveryService.Setup(eds => eds.SendTemplatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryReceipt());

        var result = await _application.SendEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryService.Verify(
            eds => eds.SendTemplatedAsync(_caller.Object, "atemplateid", "asubject",
                It.IsAny<Dictionary<string, string>>(),
                "arecipient@company.com", "arecipient", "asender@company.com", "asender",
                new List<string> { "atag", "anothertag" },
                It.IsAny<CancellationToken>()));
        _emailDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.EmailAddress == "arecipient@company.com"
            && root.Recipient.Value.DisplayName == "arecipient"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsSent == true
        ), true, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllDeliveredEmails_ThenReturnsEmails()
    {
        var datum = DateTime.UtcNow;
        var delivery = new EmailDelivery
        {
            Id = "anid",
            Body = "abody",
            Sent = datum,
            SendFailed = Optional<DateTime?>.None,
            Attempts = SendingAttempts.Create([datum]).Value,
            MessageId = "amessageid",
            Subject = "asubject",
            ToDisplayName = "arecipient",
            ToEmailAddress = "arecipient@company.com",
            LastAttempted = datum
        };
        _emailDeliveryRepository.Setup(edr =>
                edr.SearchAllDeliveriesAsync(It.IsAny<DateTime?>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailDelivery> { delivery });

        var result = await _application.SearchAllEmailDeliveriesAsync(_caller.Object, null, null, new SearchOptions(),
            new GetOptions(), CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("anid");
        result.Value.Results[0].Attempts.Should().OnlyContain(x => x.IsNear(datum));
        result.Value.Results[0].Subject.Should().Be("asubject");
        result.Value.Results[0].Body.Should().Be("abody");
        result.Value.Results[0].ToEmailAddress.Should().Be("arecipient@company.com");
        result.Value.Results[0].ToDisplayName.Should().Be("arecipient");
        result.Value.Results[0].IsSent.Should().BeTrue();
    }

    [Fact]
    public async Task WhenConfirmEmailDeliveredAsyncAndReceiptNotExist_ThenReturns()
    {
        _emailDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EmailDeliveryRoot>.None);

        var result = await _application.ConfirmEmailDeliveredAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmEmailDeliveredAsyncAndAlreadyDelivered_ThenReturns()
    {
        var messageId = CreateMessageId();
        var email = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        email.SetContent("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value,
            new List<string> { "atag" });
        email.SucceededSending("areceiptid");
        email.ConfirmDelivery("areceiptid", DateTime.UtcNow);
        _emailDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email.ToOptional());

        var result = await _application.ConfirmEmailDeliveredAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmEmailDeliveredAsync_ThenDelivers()
    {
        var messageId = CreateMessageId();
        var email = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        email.SetContent("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value,
            new List<string> { "atag" });
        email.AttemptSending();
        email.SucceededSending("areceiptid");
        _emailDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email.ToOptional());

        var result = await _application.ConfirmEmailDeliveredAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryRepository.Verify(rep => rep.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.IsSent == true
            && root.IsDelivered == true
            && root.Delivered.Value.IsNear(DateTime.UtcNow)
            && root.IsFailedDelivery == false
        ), false, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenConfirmEmailDeliveryFailedAsyncAndReceiptNotExist_ThenReturns()
    {
        _emailDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EmailDeliveryRoot>.None);

        var result = await _application.ConfirmEmailDeliveryFailedAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, "areason", CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmEmailDeliveryFailedAsyncAndAlreadyDelivered_ThenReturns()
    {
        var messageId = CreateMessageId();
        var email = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        email.SetContent("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value,
            new List<string> { "atag" });
        email.SucceededSending("areceiptid");
        email.ConfirmDelivery("areceiptid", DateTime.UtcNow);
        _emailDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email.ToOptional());

        var result = await _application.ConfirmEmailDeliveryFailedAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, "areason", CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmEmailDeliveryFailedAsync_ThenDelivers()
    {
        var messageId = CreateMessageId();
        var email = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        email.SetContent("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value,
            new List<string> { "atag" });
        email.AttemptSending();
        email.SucceededSending("areceiptid");
        _emailDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email.ToOptional());

        var result = await _application.ConfirmEmailDeliveryFailedAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, "areason", CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryRepository.Verify(rep => rep.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.IsSent == true
            && root.IsDelivered == false
            && root.IsFailedDelivery == true
            && root.FailedDelivery.Value.IsNear(DateTime.UtcNow)
        ), false, It.IsAny<CancellationToken>()));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllEmailsAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _emailMessageQueue.Setup(emq =>
                emq.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _application.DrainAllEmailsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _emailMessageQueue.Verify(
            emq => emq.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllEmailsAsyncAndSomeOnQueue_ThenDeliversAll()
    {
        var message1Id = CreateMessageId();
        var message1 = new EmailMessage
        {
            MessageId = message1Id,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject1",
                Body = "abody1",
                ToEmailAddress = "arecipient1@company.com",
                ToDisplayName = "arecipient1",
                FromEmailAddress = "asender1@company.com",
                FromDisplayName = "asender1",
                Tags = ["atag", "anothertag"]
            }
        };
        var message2Id = CreateMessageId();
        var message2 = new EmailMessage
        {
            MessageId = message2Id,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject2",
                Body = "abody2",
                ToEmailAddress = "arecipient2@company.com",
                ToDisplayName = "arecipient2",
                FromEmailAddress = "asender2@company.com",
                FromDisplayName = "asender2",
                Tags = ["atag", "anothertag"]
            }
        };
        var callbackCount = 1;
        _emailMessageQueue.Setup(emq =>
                emq.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((Func<EmailMessage, CancellationToken, Task<Result<Error>>> action, CancellationToken _) =>
            {
                if (callbackCount == 1)
                {
                    action(message1, CancellationToken.None);
                }

                if (callbackCount == 2)
                {
                    action(message2, CancellationToken.None);
                }
            })
            .Returns((Func<EmailMessage, CancellationToken, Task<Result<Error>>> _, CancellationToken _) =>
            {
                callbackCount++;
                return Task.FromResult<Result<bool, Error>>(callbackCount is 1 or 2);
            });

        var result = await _application.DrainAllEmailsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _emailMessageQueue.Verify(
            emq => emq.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), "asubject1", "abody1", "arecipient1@company.com",
                "arecipient1", "asender1@company.com", "asender1", new List<string> { "atag", "anothertag" },
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), "asubject2", "abody2", "arecipient2@company.com",
                "arecipient2", "asender2@company.com", "asender2", new List<string> { "atag", "anothertag" },
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            eds => eds.SendHtmlAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
#endif

    private static string CreateMessageId()
    {
        return new MessageQueueIdFactory().Create("aqueuename");
    }
}