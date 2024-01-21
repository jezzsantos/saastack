using System.Net;
using AncillaryInfrastructure.IntegrationTests.Stubs;
using ApiHost1;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common;
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
    public async Task WhenDeliverEmailAndDeliverySucceeds_ThenDelivered()
    {
        _emailDeliveryService.DeliverySucceeds = true;
        var request = new DeliverEmailRequest
        {
            Message = new EmailMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Html = new QueuedEmailHtmlMessage
                {
                    Subject = "asubject",
                    HtmlBody = "anhtmlbody",
                    ToEmailAddress = "arecipient@company.com",
                    ToDisplayName = "atodisplayname",
                    FromEmailAddress = "asender@company.com",
                    FromDisplayName = "afromdisplayname"
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsDelivered.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeTrue();
    }

    [Fact]
    public async Task WhenDeliverEmailAndDeliveryFails_ThenNotDelivered()
    {
        _emailDeliveryService.DeliverySucceeds = false;
        var request = new DeliverEmailRequest
        {
            Message = new EmailMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Html = new QueuedEmailHtmlMessage
                {
                    Subject = "asubject",
                    HtmlBody = "anhtmlbody",
                    ToEmailAddress = "arecipient@company.com",
                    ToDisplayName = "atodisplayname",
                    FromEmailAddress = "asender@company.com",
                    FromDisplayName = "afromdisplayname"
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _emailDeliveryService.LastSubject.Should().Be("asubject");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDeliverEmailAndDeliveryFailsFirstTimeAndSucceedsSecondTime_ThenDelivered()
    {
        _emailDeliveryService.DeliverySucceeds = false;
        var request = new DeliverEmailRequest
        {
            Message = new EmailMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Html = new QueuedEmailHtmlMessage
                {
                    Subject = "asubject",
                    HtmlBody = "anhtmlbody",
                    ToEmailAddress = "arecipient@company.com",
                    ToDisplayName = "atodisplayname",
                    FromEmailAddress = "asender@company.com",
                    FromDisplayName = "afromdisplayname"
                }
            }.ToJson()!
        };
        var firstAttempt = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        firstAttempt.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        _emailDeliveryService.DeliverySucceeds = true;
        var secondAttempt = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        secondAttempt.Content.Value.IsDelivered.Should().BeTrue();

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should().HaveCount(2);
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeTrue();
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
        var messageId1 = CreateMessageId();
        var messageId2 = CreateMessageId();
        var messageId3 = CreateMessageId();
        await _emailMessageQueue.PushAsync(call, new EmailMessage
        {
            MessageId = messageId1,
            CallId = "acallid",
            CallerId = "acallerid",
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject1",
                HtmlBody = "anhtmlbody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "atodisplayname",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "afromdisplayname"
            }
        }, CancellationToken.None);
        await _emailMessageQueue.PushAsync(call, new EmailMessage
        {
            MessageId = messageId2,
            CallId = "acallid",
            CallerId = "acallerid",
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject2",
                HtmlBody = "anhtmlbody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "atodisplayname",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "afromdisplayname"
            }
        }, CancellationToken.None);
        await _emailMessageQueue.PushAsync(call, new EmailMessage
        {
            MessageId = messageId3,
            CallId = "acallid",
            CallerId = "acallerid",
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject3",
                HtmlBody = "anhtmlbody",
                ToEmailAddress = "arecipient@company.com",
                ToDisplayName = "atodisplayname",
                FromEmailAddress = "asender@company.com",
                FromDisplayName = "afromdisplayname"
            }
        }, CancellationToken.None);

        var request = new DrainAllEmailsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _emailDeliveryService.AllSubjects.Count.Should().Be(3);
        _emailDeliveryService.AllSubjects.Should().ContainInOrder("asubject1", "asubject2", "asubject3");
    }
#endif

    private static string CreateMessageId()
    {
        return new MessageQueueIdFactory().Create("email");
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IEmailDeliveryService, StubEmailDeliveryService>();
    }
}