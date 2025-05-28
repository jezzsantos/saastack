using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Interfaces;
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
public class AncillaryApplicationSmsingSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISmsDeliveryRepository> _smsDeliveryRepository;
    private readonly Mock<ISmsDeliveryService> _smsDeliveryService;
    private readonly Mock<ISmsMessageQueue> _smsMessageQueue;

    public AncillaryApplicationSmsingSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.HostRegion)
            .Returns(DatacenterLocations.AustraliaEast);
        var usageMessageQueue = new Mock<IUsageMessageQueue>();
        var usageDeliveryService = new Mock<IUsageDeliveryService>();
        var auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        var emailMessageQueue = new Mock<IEmailMessageQueue>();
        var emailDeliveryService = new Mock<IEmailDeliveryService>();
        var emailDeliveryRepository = new Mock<IEmailDeliveryRepository>();
        _smsMessageQueue = new Mock<ISmsMessageQueue>();
        _smsDeliveryService = new Mock<ISmsDeliveryService>();
        _smsDeliveryService.Setup(eds => eds.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryReceipt());
        _smsDeliveryRepository = new Mock<ISmsDeliveryRepository>();
        _smsDeliveryRepository.Setup(ar =>
                ar.SaveAsync(It.IsAny<SmsDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmsDeliveryRoot root, bool _, CancellationToken _) => root);
        _smsDeliveryRepository.Setup(edr =>
                edr.FindByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SmsDeliveryRoot>.None);
        var provisioningMessageQueue = new Mock<IProvisioningMessageQueue>();
        var provisioningDeliveryService = new Mock<IProvisioningNotificationService>();

        _application = new AncillaryApplication(_recorder.Object, _idFactory.Object, usageMessageQueue.Object,
            usageDeliveryService.Object, auditMessageRepository.Object, auditRepository.Object,
            emailMessageQueue.Object, emailDeliveryService.Object, emailDeliveryRepository.Object,
            _smsMessageQueue.Object, _smsDeliveryService.Object, _smsDeliveryRepository.Object,
            provisioningMessageQueue.Object, provisioningDeliveryService.Object);
    }

    [Fact]
    public async Task WhenSendSmsAsyncAndMessageHasNoHtml_ThenReturnsError()
    {
        var messageAsJson = new SmsMessage
        {
            Message = null
        }.ToJson()!;

        var result = await _application.SendSmsAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_Sms_MissingMessage);
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenSendSmsAsyncAndSent_ThenSends()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new SmsMessage
        {
            MessageId = messageId,
            Message = new QueuedSmsMessage
            {
                Body = "abody",
                ToPhoneNumber = "+6498876986",
                Tags = ["atag"]
            }
        }.ToJson()!;
        var sms = SmsDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value,
                Optional<Identifier>.None, DatacenterLocations.AustraliaEast).Value;
        sms.SetSmsDetails("abody", PhoneNumber.Create("+6498876986").Value, new List<string> { "atag" });
        sms.AttemptSending();
        sms.SucceededSending("areceiptid");
        _smsDeliveryRepository.Setup(edr =>
                edr.FindByMessageIdAsync(It.IsAny<QueuedMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sms.ToOptional());
        _smsDeliveryService.Setup(eds => eds.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryReceipt());

        var result = await _application.SendSmsAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _smsDeliveryRepository.Verify(
            edr => edr.SaveAsync(It.IsAny<SmsDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenSendSmsAsyncAndNotDelivered_ThenFailsSending()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new SmsMessage
        {
            MessageId = messageId,
            Message = new QueuedSmsMessage
            {
                Body = "abody",
                ToPhoneNumber = "+6498876986"
            }
        }.ToJson()!;
        _smsDeliveryService.Setup(eds => eds.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected());

        var result = await _application.SendSmsAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected);
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), "abody", "+6498876986", It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()));
        _smsDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<SmsDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.Number == "+6498876986"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsSent == false
        ), true, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSendSmsAsyncAndAlreadyDelivered_ThenDoesNotResend()
    {
        var messageId = CreateMessageId();
        var messageAsJson = new SmsMessage
        {
            MessageId = messageId,
            Message = new QueuedSmsMessage
            {
                Body = "abody",
                ToPhoneNumber = "+6498876986"
            }
        }.ToJson()!;
        _smsDeliveryService.Setup(eds => eds.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryReceipt());

        var result = await _application.SendSmsAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), "abody", "+6498876986", It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()));
        _smsDeliveryRepository.Verify(edr => edr.SaveAsync(It.Is<SmsDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.Recipient.Value.Number == "+6498876986"
            && root.Attempts.Attempts.Count == 1
            && root.Attempts.Attempts[0].IsNear(DateTime.UtcNow)
            && root.IsSent == true
        ), true, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllDeliveredSmses_ThenReturnsSmses()
    {
        var datum = DateTime.UtcNow;
        var delivery = new SmsDelivery
        {
            Id = "anid",
            Body = "abody",
            Sent = datum,
            SendFailed = Optional<DateTime?>.None,
            Attempts = SendingAttempts.Create([datum]).Value,
            MessageId = "amessageid",
            ToPhoneNumber = "+6498876986",
            LastAttempted = datum
        };
        _smsDeliveryRepository.Setup(edr =>
                edr.SearchAllAsync(It.IsAny<DateTime?>(), It.IsAny<string?>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<SmsDelivery>([delivery]));

        var result = await _application.SearchAllSmsDeliveriesAsync(_caller.Object, null, null, null,
            new SearchOptions(),
            new GetOptions(), CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("anid");
        result.Value.Results[0].Attempts.Should().OnlyContain(x => x.IsNear(datum));
        result.Value.Results[0].Body.Should().Be("abody");
        result.Value.Results[0].ToPhoneNumber.Should().Be("+6498876986");
        result.Value.Results[0].IsSent.Should().BeTrue();
    }

    [Fact]
    public async Task WhenConfirmSmsDeliveredAsyncAndReceiptNotExist_ThenReturns()
    {
        _smsDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SmsDeliveryRoot>.None);

        var result = await _application.ConfirmSmsDeliveredAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<SmsDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmSmsDeliveredAsyncAndAlreadyDelivered_ThenReturns()
    {
        var messageId = CreateMessageId();
        var sms = SmsDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value,
                Optional<Identifier>.None, DatacenterLocations.AustraliaEast).Value;
        sms.SetSmsDetails("abody", PhoneNumber.Create("+6498876986").Value, new List<string> { "atag" });
        sms.SucceededSending("areceiptid");
        sms.ConfirmDelivery("areceiptid", DateTime.UtcNow);
        _smsDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sms.ToOptional());

        var result = await _application.ConfirmSmsDeliveredAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<SmsDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmSmsDeliveredAsync_ThenDelivers()
    {
        var messageId = CreateMessageId();
        var sms = SmsDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value,
                Optional<Identifier>.None, DatacenterLocations.AustraliaEast).Value;
        sms.SetSmsDetails("abody", PhoneNumber.Create("+6498876986").Value, new List<string> { "atag" });
        sms.AttemptSending();
        sms.SucceededSending("areceiptid");
        _smsDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sms.ToOptional());

        var result = await _application.ConfirmSmsDeliveredAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryRepository.Verify(rep => rep.SaveAsync(It.Is<SmsDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.IsSent == true
            && root.IsDelivered == true
            && root.Delivered.Value.IsNear(DateTime.UtcNow)
            && root.IsFailedDelivery == false
        ), false, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenConfirmSmsDeliveryFailedAsyncAndReceiptNotExist_ThenReturns()
    {
        _smsDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SmsDeliveryRoot>.None);

        var result = await _application.ConfirmSmsDeliveryFailedAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, "areason", CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<SmsDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmSmsDeliveryFailedAsyncAndAlreadyDelivered_ThenReturns()
    {
        var messageId = CreateMessageId();
        var sms = SmsDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value,
                Optional<Identifier>.None, DatacenterLocations.AustraliaEast).Value;
        sms.SetSmsDetails("abody", PhoneNumber.Create("+6498876986").Value, new List<string> { "atag" });
        sms.SucceededSending("areceiptid");
        sms.ConfirmDelivery("areceiptid", DateTime.UtcNow);
        _smsDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sms.ToOptional());

        var result = await _application.ConfirmSmsDeliveryFailedAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, "areason", CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<SmsDeliveryRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmSmsDeliveryFailedAsync_ThenDelivers()
    {
        var messageId = CreateMessageId();
        var sms = SmsDeliveryRoot
            .Create(_recorder.Object, _idFactory.Object, QueuedMessageId.Create(messageId).Value,
                Optional<Identifier>.None, DatacenterLocations.AustraliaEast).Value;
        sms.SetSmsDetails("abody", PhoneNumber.Create("+6498876986").Value, new List<string> { "atag" });
        sms.AttemptSending();
        sms.SucceededSending("areceiptid");
        _smsDeliveryRepository.Setup(rep =>
                rep.FindByReceiptIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sms.ToOptional());

        var result = await _application.ConfirmSmsDeliveryFailedAsync(_caller.Object, "areceiptid",
            DateTime.UtcNow, "areason", CancellationToken.None);

        result.Should().BeSuccess();
        _smsDeliveryRepository.Verify(rep => rep.SaveAsync(It.Is<SmsDeliveryRoot>(root =>
            root.MessageId == messageId
            && root.IsSent == true
            && root.IsDelivered == false
            && root.IsFailedDelivery == true
            && root.FailedDelivery.Value.IsNear(DateTime.UtcNow)
        ), false, It.IsAny<CancellationToken>()));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllSmsesAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _smsMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<SmsMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _application.DrainAllSmsesAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _smsMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<SmsMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllSmsesAsyncAndSomeOnQueue_ThenDeliversAll()
    {
        var message1Id = CreateMessageId();
        var message1 = new SmsMessage
        {
            MessageId = message1Id,
            Message = new QueuedSmsMessage
            {
                Body = "abody1",
                ToPhoneNumber = "+6498876986",
                Tags = ["atag", "anothertag"]
            }
        };
        var message2Id = CreateMessageId();
        var message2 = new SmsMessage
        {
            MessageId = message2Id,
            Message = new QueuedSmsMessage
            {
                Body = "abody2",
                ToPhoneNumber = "+6498876986",
                Tags = ["atag", "anothertag"]
            }
        };
        var callbackCount = 1;
        _smsMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<SmsMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((Func<SmsMessage, CancellationToken, Task<Result<Error>>> action, CancellationToken _) =>
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
            .Returns((Func<SmsMessage, CancellationToken, Task<Result<Error>>> _, CancellationToken _) =>
            {
                callbackCount++;
                return Task.FromResult<Result<bool, Error>>(callbackCount is 1 or 2);
            });

        var result = await _application.DrainAllSmsesAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _smsMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<SmsMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), "abody1", "+6498876986", It.Is<IReadOnlyList<string>>(
                tags =>
                    tags.Count == 2
                    && tags[0] == "atag"
                    && tags[1] == "anothertag"), It.IsAny<CancellationToken>()));
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), "abody2", "+6498876986", It.Is<IReadOnlyList<string>>(
                tags =>
                    tags.Count == 2
                    && tags[0] == "atag"
                    && tags[1] == "anothertag"), It.IsAny<CancellationToken>()));
        _smsDeliveryService.Verify(
            urs => urs.SendAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
#endif

    private static string CreateMessageId()
    {
        return new MessageQueueMessageIdFactory().Create("aqueuename");
    }
}