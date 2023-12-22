using AncillaryApplication.Persistence;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Services.Shared;
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
public class AncillaryApplicationSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<IAuditMessageQueueRepository> _auditMessageRepository;
    private readonly Mock<IAuditRepository> _auditRepository;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IUsageMessageQueueRepository> _usageMessageRepository;
    private readonly Mock<IUsageReportingService> _usageReportingService;

    public AncillaryApplicationSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        _usageMessageRepository = new Mock<IUsageMessageQueueRepository>();
        _usageReportingService = new Mock<IUsageReportingService>();
        _auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        _auditRepository = new Mock<IAuditRepository>();
        _auditRepository.Setup(ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()))
            .Returns((AuditRoot root, CancellationToken _) => Task.FromResult<Result<AuditRoot, Error>>(root));

        _application = new AncillaryApplication(recorder.Object, idFactory.Object, _usageMessageRepository.Object,
            _usageReportingService.Object, _auditMessageRepository.Object, _auditRepository.Object);
    }

    [Fact]
    public async Task WhenDeliverUsageAsyncAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result = await _application.DeliverUsageAsync(_caller.Object, "anunknownmessage", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.AncillaryApplication_InvalidQueuedMessage.Format(nameof(UsageMessage), "anunknownmessage"));
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
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
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
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
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverUsageAsync_ThenDelivers()
    {
        var messageAsJson = new UsageMessage
        {
            ForId = "aforid",
            EventName = "aneventname",
            Context = new Dictionary<string, string>
            {
                { "aname", "avalue" }
            }
        }.ToJson()!;

        var result = await _application.DeliverUsageAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), "aforid", "aneventname",
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

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllUsagesAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _usageMessageRepository.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result = await _application.DrainAllUsagesAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _usageMessageRepository.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
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
        _usageMessageRepository.Setup(umr =>
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
        _usageMessageRepository.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<UsageMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), "aforid1", "aneventname1",
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), "aforid2", "aneventname2",
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));
        _usageReportingService.Verify(
            urs => urs.TrackAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
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
#endif
}