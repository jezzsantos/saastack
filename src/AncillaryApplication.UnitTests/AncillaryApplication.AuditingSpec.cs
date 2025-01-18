using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
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
public class AncillaryApplicationAuditingSpec
{
    private readonly AncillaryApplication _application;
    private readonly Mock<IAuditMessageQueueRepository> _auditMessageRepository;
    private readonly Mock<IAuditRepository> _auditRepository;
    private readonly Mock<ICallerContext> _caller;

    public AncillaryApplicationAuditingSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns(new Result<Identifier, Error>("anid".ToId()));
        _caller = new Mock<ICallerContext>();
        var usageMessageQueue = new Mock<IUsageMessageQueue>();
        var usageDeliveryService = new Mock<IUsageDeliveryService>();
        _auditMessageRepository = new Mock<IAuditMessageQueueRepository>();
        _auditRepository = new Mock<IAuditRepository>();
        _auditRepository.Setup(ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditRoot root, CancellationToken _) => root);
        var emailMessageQueue = new Mock<IEmailMessageQueue>();
        var emailDeliveryService = new Mock<IEmailDeliveryService>();
        var emailDeliveryRepository = new Mock<IEmailDeliveryRepository>();
        var smsMessageQueue = new Mock<ISmsMessageQueue>();
        var smsDeliveryService = new Mock<ISmsDeliveryService>();
        var smsDeliveryRepository = new Mock<ISmsDeliveryRepository>();
        var provisioningMessageQueue = new Mock<IProvisioningMessageQueue>();
        var provisioningDeliveryService = new Mock<IProvisioningNotificationService>();

        _application = new AncillaryApplication(recorder.Object, idFactory.Object, usageMessageQueue.Object,
            usageDeliveryService.Object, _auditMessageRepository.Object, _auditRepository.Object,
            emailMessageQueue.Object, emailDeliveryService.Object, emailDeliveryRepository.Object,
            smsMessageQueue.Object, smsDeliveryService.Object, smsDeliveryRepository.Object,
            provisioningMessageQueue.Object, provisioningDeliveryService.Object);
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
            Resources.AncillaryApplication_Audit_MissingCode);
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
            Arguments = ["anarg1", "anarg2"],
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
    public async Task WhenSearchAllDeliveredEmails_ThenReturnsEmails()
    {
        var datum = DateTime.UtcNow;
        var audit = new Audit
        {
            Id = "anid",
            OrganizationId = "anorganizationid",
            AgainstId = "anagainstid",
            AuditCode = "anauditcode",
            MessageTemplate = "amessagetemplate",
            TemplateArguments = TemplateArguments.Create(["anarg1", "anarg2"]).Value
        };
        _auditRepository.Setup(edr =>
                edr.SearchAllAsync(It.IsAny<DateTime?>(), It.IsAny<string?>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Audit> { audit });

        var result = await _application.SearchAllAuditsAsync(_caller.Object, null, null,
            new SearchOptions(), new GetOptions(), CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("anid");
        result.Value.Results[0].OrganizationId.Should().Be("anorganizationid");
        result.Value.Results[0].AgainstId.Should().Be("anagainstid");
        result.Value.Results[0].AuditCode.Should().Be("anauditcode");
        result.Value.Results[0].MessageTemplate.Should().Be("amessagetemplate");
        result.Value.Results[0].TemplateArguments.Count.Should().Be(2);
        result.Value.Results[0].TemplateArguments[0].Should().Be("anarg1");
        result.Value.Results[0].TemplateArguments[1].Should().Be("anarg2");
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllAuditsAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _auditMessageRepository.Setup(umr =>
                umr.PopSingleAsync(It.IsAny<Func<AuditMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _application.DrainAllAuditsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _auditMessageRepository.Verify(
            urs => urs.PopSingleAsync(It.IsAny<Func<AuditMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _auditRepository.Verify(ar => ar.SaveAsync(It.IsAny<AuditRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

#if TESTINGONLY
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