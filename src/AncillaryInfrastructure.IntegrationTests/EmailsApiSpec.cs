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
using UnitTesting.Common.Validation;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class EmailsApiSpec : WebApiSpec<Program>
{
    private readonly StubEmailDeliveryService _emailDeliveryService;
    private readonly IEmailMessageQueue _emailMessageQueue;

    public EmailsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _emailDeliveryService = setup.GetRequiredService<IEmailDeliveryService>().As<StubEmailDeliveryService>();
        _emailDeliveryService.Reset();
        _emailMessageQueue = setup.GetRequiredService<IEmailMessageQueue>();
#if TESTINGONLY
        _emailMessageQueue.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
#endif
    }

    [Fact]
    public async Task WhenSendEmailAndDeliverySucceeds_ThenDelivered()
    {
        _emailDeliveryService.SendingSucceeds = true;
        var request = new SendEmailRequest
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
                    FromDisplayName = "afromdisplayname",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsSent.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Emails[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Emails[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Emails[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenSendEmailAndDeliveryFails_ThenNotDelivered()
    {
        _emailDeliveryService.SendingSucceeds = false;
        var request = new SendEmailRequest
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
                    FromDisplayName = "afromdisplayname",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _emailDeliveryService.LastSubject.Should().Be("asubject");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(now, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Emails[0].IsSent.Should().BeFalse();
        deliveries.Content.Value.Emails[0].SentAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Emails[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Emails[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenSendEmailAndDeliveryFailsFirstTimeAndSucceedsSecondTime_ThenDelivered()
    {
        _emailDeliveryService.SendingSucceeds = false;
        var request = new SendEmailRequest
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
                    FromDisplayName = "afromdisplayname",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var firstAttempt = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        firstAttempt.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        _emailDeliveryService.SendingSucceeds = true;
        var secondAttempt = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        secondAttempt.Content.Value.IsSent.Should().BeTrue();

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should().HaveCount(2);
        deliveries.Content.Value.Emails[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Emails[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Emails[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenConfirmDelivery_ThenDelivered()
    {
        _emailDeliveryService.SendingSucceeds = true;
        var request = new SendEmailRequest
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
                    FromDisplayName = "afromdisplayname",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var sent = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        sent.Content.Value.IsSent.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");
        var receiptId = _emailDeliveryService.LastReceiptId;

#if TESTINGONLY
        await Api.PostAsync(new ConfirmEmailDeliveredRequest
        {
            ReceiptId = receiptId
        });
#endif

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(now, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Emails[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeTrue();
        deliveries.Content.Value.Emails[0].DeliveredAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Emails[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Emails[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenConfirmDeliveryFailed_ThenFailsDelivery()
    {
        _emailDeliveryService.SendingSucceeds = true;
        var request = new SendEmailRequest
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
                    FromDisplayName = "afromdisplayname",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var sent = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        sent.Content.Value.IsSent.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");
        var receiptId = _emailDeliveryService.LastReceiptId;

#if TESTINGONLY
        await Api.PostAsync(new ConfirmEmailDeliveryFailedRequest
        {
            ReceiptId = receiptId,
            Reason = "areason"
        });
#endif

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Emails[0].ToEmailAddress.Should().Be("arecipient@company.com");
        deliveries.Content.Value.Emails[0].ToDisplayName.Should().Be("atodisplayname");
        deliveries.Content.Value.Emails[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(now, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Emails[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Emails[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Emails[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Emails[0].IsDeliveryFailed.Should().BeTrue();
        deliveries.Content.Value.Emails[0].FailedDeliveryAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Emails[0].FailedDeliveryReason.Should().Be("areason");
        deliveries.Content.Value.Emails[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenSearchEmailDeliveriesWithTags_TheReturnsEmails()
    {
        _emailDeliveryService.SendingSucceeds = true;
        var request = new SendEmailRequest
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
                    FromDisplayName = "afromdisplayname",
                    Tags = ["atag1", "atag2", "atag3"]
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsSent.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest
            {
                Tags = ["atag2", "atag3"]
            },
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails!.Count.Should().Be(1);
        deliveries.Content.Value.Emails[0].Subject.Should().Be("asubject");
        deliveries.Content.Value.Emails[0].Tags.Count.Should().Be(3);
        deliveries.Content.Value.Emails[0].Tags[0].Should().Be("atag1");
        deliveries.Content.Value.Emails[0].Tags[1].Should().Be("atag2");
        deliveries.Content.Value.Emails[0].Tags[2].Should().Be("atag3");
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
                FromDisplayName = "afromdisplayname",
                Tags = ["atag"]
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
                FromDisplayName = "afromdisplayname",
                Tags = ["atag"]
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
                FromDisplayName = "afromdisplayname",
                Tags = ["atag"]
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