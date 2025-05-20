using AncillaryApplication.Persistence;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryApplication.UnitTests;

[Trait("Category", "Unit")]
public class AncillaryApplicationUsageSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IUsageDeliveryService> _usageDeliveryService;
    private readonly Mock<IUsageMessageQueue> _usageMessageQueue;

    public AncillaryApplicationUsageSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        _usageMessageQueue = new Mock<IUsageMessageQueue>();
        _usageDeliveryService = new Mock<IUsageDeliveryService>();
        var auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        var emailMessageQueue = new Mock<IEmailMessageQueue>();
        var emailDeliveryService = new Mock<IEmailDeliveryService>();
        var emailDeliveryRepository = new Mock<IEmailDeliveryRepository>();
        var smsMessageQueue = new Mock<ISmsMessageQueue>();
        var smsDeliveryService = new Mock<ISmsDeliveryService>();
        var smsDeliveryRepository = new Mock<ISmsDeliveryRepository>();
        var provisioningMessageQueue = new Mock<IProvisioningMessageQueue>();
        var provisioningDeliveryService = new Mock<IProvisioningNotificationService>();

        _application = new AncillaryApplication(recorder.Object, idFactory.Object, _usageMessageQueue.Object,
            _usageDeliveryService.Object, auditMessageRepository.Object, auditRepository.Object,
            emailMessageQueue.Object, emailDeliveryService.Object, emailDeliveryRepository.Object,
            smsMessageQueue.Object, smsDeliveryService.Object, smsDeliveryRepository.Object,
            provisioningMessageQueue.Object, provisioningDeliveryService.Object);
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
            Resources.AncillaryApplication_Usage_MissingForId);
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
            Resources.AncillaryApplication_Usage_MissingEventName);
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverUsageAsyncAndNoRegionInMessage_ThenDelivers()
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
                    dic.Count == 2
                    && dic[UsageConstants.Properties.Region] == DatacenterLocations.Unknown.Code
                    && dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
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
                { "aname", "avalue" },
                { UsageConstants.Properties.Region, "aregion" }
            }
        }.ToJson()!;

        var result = await _application.DeliverUsageAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), "aforid", "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.Region] == "aregion"
                    && dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllUsagesAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _usageMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _application.DrainAllUsagesAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _usageMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _usageDeliveryService.Verify(
            urs => urs.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

#if TESTINGONLY
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
#endif
}