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
public class AncillaryApplicationSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<IAuditMessageQueueRepository> _auditMessageRepository;
    private readonly Mock<IAuditRepository> _auditRepository;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEmailDeliveryRepository> _emailDeliveryRepository;
    private readonly Mock<IEmailDeliveryService> _emailDeliveryService;
    private readonly Mock<IEmailMessageQueue> _emailMessageQueue;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IUsageDeliveryService> _usageDeliveryService;
    private readonly Mock<IUsageMessageQueue> _usageMessageQueue;

    public AncillaryApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        _usageMessageQueue = new Mock<IUsageMessageQueue>();
        _usageDeliveryService = new Mock<IUsageDeliveryService>();
        _auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        _auditRepository = new Mock<IAuditRepository>();
        _auditRepository.Setup(ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()))
            .Returns((AuditRoot root, CancellationToken _) => Task.FromResult<Result<AuditRoot, Error>>(root));
        _emailMessageQueue = new Mock<IEmailMessageQueue>();
        _emailDeliveryService = new Mock<IEmailDeliveryService>();
        _emailDeliveryService.Setup(eds => eds.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt()));
        _emailDeliveryRepository = new Mock<IEmailDeliveryRepository>();
        _emailDeliveryRepository.Setup(ar =>
                ar.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns((EmailDeliveryRoot root, bool _, CancellationToken _) =>
                Task.FromResult<Result<EmailDeliveryRoot, Error>>(root));
        _emailDeliveryRepository.Setup(edr =>
                edr.FindDeliveryByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<EmailDeliveryRoot>, Error>>(Optional<EmailDeliveryRoot>.None));

        _application = new AncillaryApplication(_recorder.Object, _idFactory.Object, _usageMessageQueue.Object,
            _usageDeliveryService.Object, _auditMessageRepository.Object, _auditRepository.Object,
            _emailMessageQueue.Object, _emailDeliveryService.Object, _emailDeliveryRepository.Object);
    }

    [Fact]
    public async Task WhenDeliverUsageAsyncAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result = await _application.DeliverUsageAsync(_caller.Object, "anunknownmessage", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_InvalidQueuedMessage.Format(nameof(UsageMessage), "anunknownmessage"));
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverUsageAsyncAndMessageHasNoForId_ThenReturnsError()
    {
        var messageAsJson = new UsageMessage
        {
            ForId = null,
            EventName = "aneventname"
        }.ToJson()!;

        var result = await _application.DeliverUsageAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_MissingUsageForId);
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverUsageAsyncAndMessageHasNoEventName_ThenReturnsError()
    {
        var messageAsJson = new UsageMessage
        {
            ForId = "aforid",
            EventName = null
        }.ToJson()!;

        var result = await _application.DeliverUsageAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_MissingUsageEventName);
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverUsageAsync_ThenDelivers()
    {
        var messageAsJson = new UsageMessage
        {
            ForId = "aforid",
            EventName = "aneventname",
            Additional = new Dictionary<string, string>
            {
                { "aname", "avalue" }
            }
        }.ToJson()!;

        var result = await _application.DeliverUsageAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "aforid", "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 1
                    && dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeliverAuditAsyncAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result = await _application.DeliverAuditAsync(_caller.Object, "anunknownmessage", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_InvalidQueuedMessage.Format(nameof(AuditMessage), "anunknownmessage"));
        _auditRepository.Verify(
            ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverAuditAsyncAndMessageHasNoAuditCode_ThenReturnsError()
    {
        var messageAsJson = new AuditMessage
        {
            AuditCode = null
        }.ToJson()!;

        var result = await _application.DeliverAuditAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_MissingAuditCode);
        _auditRepository.Verify(
            ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverAuditAsync_ThenDelivers()
    {
        var messageAsJson = new AuditMessage
        {
            AuditCode = "anauditcode",
            AgainstId = "anagainstid",
            Arguments = new List<string> { "anarg1", "anarg2" },
            MessageTemplate = "amessagetemplate",
            TenantId = "atenantid"
        }.ToJson()!;

        var result = await _application.DeliverAuditAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _auditRepository.Verify(
            ar => ar.SaveAsync(It.Is<AuditRoot>(aud =>
                aud.AuditCode == "anauditcode"
                && aud.AgainstId == "anagainstid".ToId()
                && aud.MessageTemplate == "amessagetemplate"
                && aud.TemplateArguments.Value.Items.Count == 2
                && aud.TemplateArguments.Value.Items[0] == "anarg1"
                && aud.TemplateArguments.Value.Items[1] == "anarg2"
                && aud.OrganizationId.Value == "atenantid"
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeliverEmailAsyncAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result = await _application.DeliverEmailAsync(_caller.Object, "anunknownmessage", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_InvalidQueuedMessage.Format(nameof(EmailMessage), "anunknownmessage"));
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverEmailAsyncAndMessageHasNoHtml_ThenReturnsError()
    {
        var messageAsJson = new EmailMessage
        {
            Html = null
        }.ToJson()!;

        var result = await _application.DeliverEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_MissingEmailHtml);
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverEmailAsyncAndDelivered_ThenDeliversSuccessfully()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                HtmlBody = "abody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender"
            }
        }.ToJson()!;
        var root = EmailDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value).Value;
        root.SetEmailDetails("asubject", "abody",
            EmailRecipient.Create(EmailAddress.Create("arecipient@company.com").Value, "adisplayname").Value);
        root.AttemptDelivery();
        root.SucceededDelivery("atransactionid");
        _emailDeliveryRepository.Setup(edr =>
                edr.FindDeliveryByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<EmailDeliveryRoot>, Error>>(root.ToOptional()));
        _emailDeliveryService.Setup(eds => eds.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt()));

        var result = await _application.DeliverEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _emailDeliveryRepository.Verify(
            edr => edr.SaveAsync(It.IsAny<EmailDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenDeliverEmailAsyncAndNotDelivered_ThenFailsDelivery()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                HtmlBody = "abody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender"
            }
        }.ToJson()!;
        _emailDeliveryService.Setup(eds => eds.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EmailDeliveryReceipt, Error>>(Error.Unexpected()));

        var result = await _application.DeliverEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected);
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "asubject", "abody", "arecipient@company.com",
                "arecipient", "asender@company.com", "asender",
                It.IsAny<CancellationToken>()));
        _emailDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.EmailAddress == "arecipient@company.com"
            && root.Recipient.Value.DisplayName == "arecipient"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsDelivered == false
        ), true, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeliverEmailAsyncAndAlreadyDelivered_ThenDoesNotRedeliver()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new EmailMessage
        {
            MessageId = messageId,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                HtmlBody = "abody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "arecipient",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "asender"
            }
        }.ToJson()!;
        _emailDeliveryService.Setup(eds => eds.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt()));

        var result = await _application.DeliverEmailAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "asubject", "abody", "arecipient@company.com",
                "arecipient", "asender@company.com", "asender",
                It.IsAny<CancellationToken>()));
        _emailDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<EmailDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.EmailAddress == "arecipient@company.com"
            && root.Recipient.Value.DisplayName == "arecipient"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsDelivered == true
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
            Delivered = datum,
            Failed = Optional<DateTime?>.None,
            Attempts = DeliveryAttempts.Create(new List<DateTime> { datum }).Value,
            MessageId = "amessageid",
            Subject = "asubject",
            ToDisplayName = "arecipient",
            ToEmailAddress = "arecipient@company.com",
            LastAttempted = datum
        };
        _emailDeliveryRepository.Setup(edr =>
                edr.SearchAllDeliveriesAsync(It.IsAny<DateTime>(), It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<EmailDelivery>, Error>>(new List<EmailDelivery> { delivery }));

        var result = await _application.SearchAllEmailDeliveriesAsync(_caller.Object, null, new SearchOptions(),
            new GetOptions(), CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("anid");
        result.Value.Results[0].Attempts.Should().ContainSingle(x => x.IsNear(datum));
        result.Value.Results[0].Subject.Should().Be("asubject");
        result.Value.Results[0].Body.Should().Be("abody");
        result.Value.Results[0].ToEmailAddress.Should().Be("arecipient@company.com");
        result.Value.Results[0].ToDisplayName.Should().Be("arecipient");
        result.Value.Results[0].IsDelivered.Should().BeTrue();
    }

    private static string CreateMessageId()
    {
        return new MessageQueueIdFactory().Create("aqueuename");
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllUsagesAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _usageMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result = await _application.DrainAllUsagesAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _usageMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDrainAllUsagesAsyncAndSomeOnQueue_ThenDeliversAll()
    {
        var message1 = new UsageMessage
        {
            ForId = "aforid1",
            EventName = "aneventname1"
        };
        var message2 = new UsageMessage
        {
            ForId = "aforid2",
            EventName = "aneventname2"
        };
        var callbackCount = 1;
        _usageMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((Func<UsageMessage, CancellationToken, Task<Result<Error>>> action, CancellationToken _) =>
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
            .Returns((Func<UsageMessage, CancellationToken, Task<Result<Error>>> _, CancellationToken _) =>
            {
                callbackCount++;
                return Task.FromResult<Result<bool, Error>>(callbackCount is 1 or 2);
            });

        var result = await _application.DrainAllUsagesAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _usageMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "aforid1", "aneventname1",
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "aforid2", "aneventname2",
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task WhenDrainAllAuditsAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _auditMessageRepository.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<AuditMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result = await _application.DrainAllAuditsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _auditMessageRepository.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<AuditMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _auditRepository.Verify(ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDrainAllAuditsAsyncAndSomeOnQueue_ThenDeliversAll()
    {
        var message1 = new AuditMessage
        {
            TenantId = "atenantid",
            AuditCode = "anauditcode1"
        };
        var message2 = new AuditMessage
        {
            TenantId = "atenantid",
            AuditCode = "anauditcode2"
        };
        var callbackCount = 1;
        _auditMessageRepository.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<AuditMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((Func<AuditMessage, CancellationToken, Task<Result<Error>>> action, CancellationToken _) =>
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
            .Returns((Func<AuditMessage, CancellationToken, Task<Result<Error>>> _, CancellationToken _) =>
            {
                callbackCount++;
                return Task.FromResult<Result<bool, Error>>(callbackCount is 1 or 2);
            });

        var result = await _application.DrainAllAuditsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _auditMessageRepository.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<AuditMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _auditRepository.Verify(ar => ar.SaveAsync(It.Is<AuditRoot>(aud =>
            aud.AuditCode == "anauditcode1"
        ), It.IsAny<CancellationToken>()));
        _auditRepository.Verify(ar => ar.SaveAsync(It.Is<AuditRoot>(aud =>
            aud.AuditCode == "anauditcode2"
        ), It.IsAny<CancellationToken>()));
        _auditRepository.Verify(ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task WhenDrainAllEmailsAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _emailMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result = await _application.DrainAllEmailsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _emailMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

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
                HtmlBody = "abody1",
                ToEmailAddress = "arecipient1@company.com",
                ToDisplayName = "arecipient1",
                FromEmailAddress = "asender1@company.com",
                FromDisplayName = "asender1"
            }
        };
        var message2Id = CreateMessageId();
        var message2 = new EmailMessage
        {
            MessageId = message2Id,
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject2",
                HtmlBody = "abody2",
                ToEmailAddress = "arecipient2@company.com",
                ToDisplayName = "arecipient2",
                FromEmailAddress = "asender2@company.com",
                FromDisplayName = "asender2"
            }
        };
        var callbackCount = 1;
        _emailMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
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
            urs => urs.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "asubject1", "abody1", "arecipient1@company.com",
                "arecipient1", "asender1@company.com", "asender1",
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "asubject2", "abody2", "arecipient2@company.com",
                "arecipient2", "asender2@company.com", "asender2",
                It.IsAny<CancellationToken>()));
        _emailDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
#endif
}