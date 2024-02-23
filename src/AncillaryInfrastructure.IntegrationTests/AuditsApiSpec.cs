using ApiHost1;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class AuditsApiSpec : WebApiSpec<Program>
{
    private readonly IAuditMessageQueueRepository _auditMessageQueue;

    public AuditsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _auditMessageQueue = setup.GetRequiredService<IAuditMessageQueueRepository>();
        _auditMessageQueue.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task WhenDeliverAudit_ThenDelivers()
    {
        var login = await LoginUserAsync(LoginUser.Operator);
        var tenantId = login.User.Profile!.DefaultOrganizationId!;

        var request = new DeliverAuditRequest
        {
            Message = new AuditMessage
            {
                MessageId = "amessageid",
                TenantId = tenantId,
                CallId = "acallid",
                CallerId = "acallerid",
                AuditCode = "anauditcode",
                AgainstId = "anagainstid",
                MessageTemplate = "amessagetemplate",
                Arguments = new List<string> { "anarg1", "anarg2" }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsDelivered.Should().BeTrue();

#if TESTINGONLY
        var audits = await Api.GetAsync(new SearchAllAuditsRequest
        {
            OrganizationId = tenantId
        });

        audits.Content.Value.Audits!.Count.Should().Be(1);
        audits.Content.Value.Audits[0].OrganizationId.Should().Be(tenantId);
        audits.Content.Value.Audits[0].MessageTemplate.Should().Be("amessagetemplate");
        audits.Content.Value.Audits[0].TemplateArguments.Count.Should().Be(2);
        audits.Content.Value.Audits[0].TemplateArguments[0].Should().Be("anarg1");
        audits.Content.Value.Audits[0].TemplateArguments[1].Should().Be("anarg2");
#endif
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllAuditsAndNone_ThenDoesNotDrainAny()
    {
        var login = await LoginUserAsync(LoginUser.Operator);
        var tenantId = login.User.Profile!.DefaultOrganizationId!;

        var request = new DrainAllAuditsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        var audits = await Api.GetAsync(new SearchAllAuditsRequest
        {
            OrganizationId = tenantId
        });

        audits.Content.Value.Audits!.Count.Should().Be(0);
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllAuditsAndSomeWithUnknownTenancies_ThenDrains()
    {
        var login = await LoginUserAsync();
        var tenantId = login.User.Profile!.DefaultOrganizationId;
        var call = CallContext.CreateCustom("acallid", "acallerid", tenantId);
        await _auditMessageQueue.PushAsync(call, new AuditMessage
        {
            MessageId = "amessageid1",
            TenantId = tenantId,
            AuditCode = "anauditcode1",
            MessageTemplate = "amessagetemplate1",
            Arguments = new List<string> { "anarg1" }
        }, CancellationToken.None);
        await _auditMessageQueue.PushAsync(call, new AuditMessage
        {
            MessageId = "amessageid2",
            TenantId = tenantId,
            AuditCode = "anauditcode2",
            MessageTemplate = "amessagetemplate2",
            Arguments = new List<string> { "anarg2" }
        }, CancellationToken.None);
        await _auditMessageQueue.PushAsync(call, new AuditMessage
        {
            MessageId = "amessageid3",
            TenantId = "anothertenantid",
            AuditCode = "anauditcode3",
            MessageTemplate = "amessagetemplate3",
            Arguments = new List<string> { "anarg3" }
        }, CancellationToken.None);

        var request = new DrainAllAuditsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        var audits = await Api.GetAsync(new SearchAllAuditsRequest
        {
            OrganizationId = tenantId
        });

        audits.Content.Value.Audits!.Count.Should().Be(2);
        audits.Content.Value.Audits[0].OrganizationId.Should().Be(tenantId);
        audits.Content.Value.Audits[0].AuditCode.Should().Be("anauditcode1");
        audits.Content.Value.Audits[0].MessageTemplate.Should().Be("amessagetemplate1");
        audits.Content.Value.Audits[0].TemplateArguments[0].Should().Be("anarg1");
        audits.Content.Value.Audits[1].OrganizationId.Should().Be(tenantId);
        audits.Content.Value.Audits[1].AuditCode.Should().Be("anauditcode2");
        audits.Content.Value.Audits[1].MessageTemplate.Should().Be("amessagetemplate2");
        audits.Content.Value.Audits[1].TemplateArguments[0].Should().Be("anarg2");
    }
#endif

    private static void OverrideDependencies(IServiceCollection services)
    {
        // nothing here yet
    }
}