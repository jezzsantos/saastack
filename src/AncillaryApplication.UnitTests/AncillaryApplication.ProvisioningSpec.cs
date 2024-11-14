using AncillaryApplication.Persistence;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryApplication.UnitTests;

[Trait("Category", "Unit")]
public class AncillaryApplicationProvisioningSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IProvisioningNotificationService> _provisioningDeliveryService;
    private readonly Mock<IProvisioningMessageQueue> _provisioningMessageQueue;

    public AncillaryApplicationProvisioningSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        var usageMessageQueue = new Mock<IUsageMessageQueue>();
        var usageDeliveryService = new Mock<IUsageDeliveryService>();
        var auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        var emailMessageQueue = new Mock<IEmailMessageQueue>();
        var emailDeliveryService = new Mock<IEmailDeliveryService>();
        var emailDeliveryRepository = new Mock<IEmailDeliveryRepository>();
        var smsMessageQueue = new Mock<ISmsMessageQueue>();
        var smsDeliveryService = new Mock<ISmsDeliveryService>();
        var smsDeliveryRepository = new Mock<ISmsDeliveryRepository>();
        _provisioningMessageQueue = new Mock<IProvisioningMessageQueue>();
        _provisioningDeliveryService = new Mock<IProvisioningNotificationService>();
        _provisioningDeliveryService.Setup(pds => pds.NotifyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        _application = new AncillaryApplication(recorder.Object, idFactory.Object, usageMessageQueue.Object,
            usageDeliveryService.Object, auditMessageRepository.Object, auditRepository.Object,
            emailMessageQueue.Object, emailDeliveryService.Object, emailDeliveryRepository.Object,
            smsMessageQueue.Object, smsDeliveryService.Object, smsDeliveryRepository.Object,
            _provisioningMessageQueue.Object, _provisioningDeliveryService.Object);
    }

    [Fact]
    public async Task WhenNotifyProvisioningAsyncAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result =
            await _application.NotifyProvisioningAsync(_caller.Object, "anunknownmessage", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_InvalidQueuedMessage.Format(nameof(ProvisioningMessage),
                "anunknownmessage"));
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyProvisioningAsyncAndMessageHasNoTenantId_ThenReturnsError()
    {
        var messageAsJson = new ProvisioningMessage
        {
            TenantId = null,
            Settings = new Dictionary<string, TenantSetting>
            {
                { "aname", new TenantSetting("avalue") }
            }
        }.ToJson()!;

        var result = await _application.NotifyProvisioningAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_Provisioning_MissingTenantId);
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyProvisioningAsync_ThenNotifies()
    {
        var messageAsJson = new ProvisioningMessage
        {
            TenantId = "atenantid",
            Settings = new Dictionary<string, TenantSetting>
            {
                { "aname1", new TenantSetting("avalue") },
                { "aname2", new TenantSetting(99) },
                { "aname3", new TenantSetting(true) }
            }
        }.ToJson()!;

        var result = await _application.NotifyProvisioningAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), "atenantid",
                It.Is<TenantSettings>(dic =>
                    dic.Count == 3
                    && dic["aname1"].As<TenantSetting>().Value.As<string>() == "avalue"
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    && dic["aname2"].As<TenantSetting>().Value.As<double>() == 99D
                    && dic["aname3"].As<TenantSetting>().Value.As<bool>() == true
                ), It.IsAny<CancellationToken>()));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllProvisioningsAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _provisioningMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _application.DrainAllProvisioningsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _provisioningMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllProvisioningsAsyncAndSomeOnQueue_ThenDeliversAll()
    {
        var message1 = new ProvisioningMessage
        {
            TenantId = "atenantid1",
            Settings = new Dictionary<string, TenantSetting>
            {
                { "aname", new TenantSetting("avalue1") }
            }
        };
        var message2 = new ProvisioningMessage
        {
            TenantId = "atenantid2",
            Settings = new Dictionary<string, TenantSetting>
            {
                { "aname", new TenantSetting("avalue2") }
            }
        };
        var callbackCount = 1;
        _provisioningMessageQueue.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>> action, CancellationToken _) =>
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
            .Returns((Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>> _, CancellationToken _) =>
            {
                callbackCount++;
                return Task.FromResult<Result<bool, Error>>(callbackCount is 1 or 2);
            });

        var result = await _application.DrainAllProvisioningsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _provisioningMessageQueue.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<ProvisioningMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), "atenantid1",
                It.Is<TenantSettings>(dic => dic["aname"].Value.As<string>() == "avalue1"),
                It.IsAny<CancellationToken>()));
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), "atenantid2",
                It.Is<TenantSettings>(dic => dic["aname"].Value.As<string>() == "avalue2"),
                It.IsAny<CancellationToken>()));
        _provisioningDeliveryService.Verify(
            urs => urs.NotifyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
#endif
}