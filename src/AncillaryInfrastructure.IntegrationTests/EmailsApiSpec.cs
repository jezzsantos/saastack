using AncillaryInfrastructure.IntegrationTests.Stubs;
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
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class EmailsApiSpec : WebApiSpec<Program>
{
    private readonly StubEmailDeliveryService _emailDeliveryService;
    private readonly IEmailMessageQueue _emailMessageQueue;

    public EmailsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
        _emailDeliveryService = setup.GetRequiredService<IEmailDeliveryService>().As<StubEmailDeliveryService>();
        _emailDeliveryService.Reset();
        _emailMessageQueue = setup.GetRequiredService<IEmailMessageQueue>();
        _emailMessageQueue.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task WhenDeliverEmail_ThenDelivers()
    {
        var request = new DeliverEmailRequest
        {
            Message = new EmailMessage
            {
                MessageId = "amessageid",
                CallId = "acallid",
                CallerId = "acallerid",
                Html = new QueuedEmailHtmlMessage
                {
                    FromDisplayName = "afromdisplayname",
                    FromEmailAddress = "afromemail",
                    HtmlBody = "anhtmlbody",
                    Subject = "asubject",
                    ToDisplayName = "atodisplayname",
                    ToEmailAddress = "atoemail"
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsDelivered.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllEmailsAndNone_ThenDoesNotDrainAny()
    {
        var request = new DrainAllEmailsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _emailDeliveryService.LastSubject.Should().BeNone();
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllEmailsAndSome_ThenDrains()
    {
        var call = CallContext.CreateCustom("acallid", "acallerid", "atenantid");
        await _emailMessageQueue.PushAsync(call, new EmailMessage
        {
            MessageId = "amessageid1",
            Html = new QueuedEmailHtmlMessage
            {
                FromDisplayName = "afromdisplayname",
                FromEmailAddress = "afromemail",
                HtmlBody = "anhtmlbody",
                Subject = "asubject1",
                ToDisplayName = "atodisplayname",
                ToEmailAddress = "atoemail"
            }
        }, CancellationToken.None);
        await _emailMessageQueue.PushAsync(call, new EmailMessage
        {
            MessageId = "amessageid2",
            Html = new QueuedEmailHtmlMessage
            {
                FromDisplayName = "afromdisplayname",
                FromEmailAddress = "afromemail",
                HtmlBody = "anhtmlbody",
                Subject = "asubject2",
                ToDisplayName = "atodisplayname",
                ToEmailAddress = "atoemail"
            }
        }, CancellationToken.None);
        await _emailMessageQueue.PushAsync(call, new EmailMessage
        {
            MessageId = "amessageid3",
            Html = new QueuedEmailHtmlMessage
            {
                FromDisplayName = "afromdisplayname",
                FromEmailAddress = "afromemail",
                HtmlBody = "anhtmlbody",
                Subject = "asubject3",
                ToDisplayName = "atodisplayname",
                ToEmailAddress = "atoemail"
            }
        }, CancellationToken.None);

        var request = new DrainAllEmailsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _emailDeliveryService.AllSubjects.Count.Should().Be(3);
        _emailDeliveryService.AllSubjects.Should().ContainInOrder("asubject1", "asubject2", "asubject3");
    }
#endif

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IEmailDeliveryService, StubEmailDeliveryService>();
    }
}