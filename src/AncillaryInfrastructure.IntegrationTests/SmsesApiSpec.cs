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
public class SmsesApiSpec : WebApiSpec<Program>
{
    private readonly StubSmsDeliveryService _smsDeliveryService;
    private readonly ISmsMessageQueue _smsMessageQueue;

    public SmsesApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _smsDeliveryService = setup.GetRequiredService<ISmsDeliveryService>().As<StubSmsDeliveryService>();
        _smsDeliveryService.Reset();
        _smsMessageQueue = setup.GetRequiredService<ISmsMessageQueue>();
#if TESTINGONLY
        _smsMessageQueue.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
#endif
    }

    [Fact]
    public async Task WhenSendSmsAndDeliverySucceeds_ThenDelivered()
    {
        _smsDeliveryService.SendingSucceeds = true;
        var request = new SendSmsRequest
        {
            Message = new SmsMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedSmsMessage
                {
                    Body = "anhtmlbody",
                    ToPhoneNumber = "+6498876986",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsSent.Should().BeTrue();
        _smsDeliveryService.LastPhoneNumber.Should().Be("+6498876986");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Smses[0].ToPhoneNumber.Should().Be("+6498876986");
        deliveries.Content.Value.Smses[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Smses[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Smses[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenSendSmsAndDeliveryFails_ThenNotDelivered()
    {
        _smsDeliveryService.SendingSucceeds = false;
        var request = new SendSmsRequest
        {
            Message = new SmsMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedSmsMessage
                {
                    Body = "anhtmlbody",
                    ToPhoneNumber = "+6498876986",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _smsDeliveryService.LastPhoneNumber.Should().Be("+6498876986");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Smses[0].ToPhoneNumber.Should().Be("+6498876986");
        deliveries.Content.Value.Smses[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(now, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Smses[0].IsSent.Should().BeFalse();
        deliveries.Content.Value.Smses[0].SentAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenSendSmsAndDeliveryFailsFirstTimeAndSucceedsSecondTime_ThenDelivered()
    {
        _smsDeliveryService.SendingSucceeds = false;
        var request = new SendSmsRequest
        {
            Message = new SmsMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedSmsMessage
                {
                    Body = "anhtmlbody",
                    ToPhoneNumber = "+6498876986",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var firstAttempt = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        firstAttempt.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        _smsDeliveryService.SendingSucceeds = true;
        var secondAttempt = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        secondAttempt.Content.Value.IsSent.Should().BeTrue();

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Smses[0].ToPhoneNumber.Should().Be("+6498876986");
        deliveries.Content.Value.Smses[0].Attempts.Should().HaveCount(2);
        deliveries.Content.Value.Smses[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Smses[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenConfirmDelivery_ThenDelivered()
    {
        _smsDeliveryService.SendingSucceeds = true;
        var request = new SendSmsRequest
        {
            Message = new SmsMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedSmsMessage
                {
                    Body = "anhtmlbody",
                    ToPhoneNumber = "+6498876986",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var sent = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        sent.Content.Value.IsSent.Should().BeTrue();
        _smsDeliveryService.LastPhoneNumber.Should().Be("+6498876986");
        var receiptId = _smsDeliveryService.LastReceiptId;

#if TESTINGONLY
        await Api.PostAsync(new ConfirmSmsDeliveredRequest
        {
            ReceiptId = receiptId
        });
#endif

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Smses[0].ToPhoneNumber.Should().Be("+6498876986");
        deliveries.Content.Value.Smses[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(now, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Smses[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Smses[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeTrue();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenConfirmDeliveryFailed_ThenFailsDelivery()
    {
        _smsDeliveryService.SendingSucceeds = true;
        var request = new SendSmsRequest
        {
            Message = new SmsMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedSmsMessage
                {
                    Body = "anhtmlbody",
                    ToPhoneNumber = "+6498876986",
                    Tags = ["atag"]
                }
            }.ToJson()!
        };
        var sent = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        sent.Content.Value.IsSent.Should().BeTrue();
        _smsDeliveryService.LastPhoneNumber.Should().Be("+6498876986");
        var receiptId = _smsDeliveryService.LastReceiptId;

#if TESTINGONLY
        await Api.PostAsync(new ConfirmSmsDeliveryFailedRequest
        {
            ReceiptId = receiptId,
            Reason = "areason"
        });
#endif

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        var now = DateTime.UtcNow;
        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Body.Should().Be("anhtmlbody");
        deliveries.Content.Value.Smses[0].ToPhoneNumber.Should().Be("+6498876986");
        deliveries.Content.Value.Smses[0].Attempts.Should()
            .ContainSingle(x => x.IsNear(now, TimeSpan.FromMinutes(1)));
        deliveries.Content.Value.Smses[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Smses[0].SentAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeTrue();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNear(now, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].FailedDeliveryReason.Should().Be("areason");
        deliveries.Content.Value.Smses[0].Tags.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Tags[0].Should().Be("atag");
    }

    [Fact]
    public async Task WhenSearchSmsDeliveriesWithTags_TheReturnsSmses()
    {
        _smsDeliveryService.SendingSucceeds = true;
        var request = new SendSmsRequest
        {
            Message = new SmsMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedSmsMessage
                {
                    Body = "anhtmlbody",
                    ToPhoneNumber = "+6498876986",
                    Tags = ["atag1", "atag2", "atag3"]
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsSent.Should().BeTrue();
        _smsDeliveryService.LastPhoneNumber.Should().Be("+6498876986");

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchSmsDeliveriesRequest
            {
                Tags = ["atag2", "atag3"]
            },
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].Tags.Count.Should().Be(3);
        deliveries.Content.Value.Smses[0].Tags[0].Should().Be("atag1");
        deliveries.Content.Value.Smses[0].Tags[1].Should().Be("atag2");
        deliveries.Content.Value.Smses[0].Tags[2].Should().Be("atag3");
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllSmsesAndNone_ThenDoesNotDrainAny()
    {
        var request = new DrainAllSmsesRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _smsDeliveryService.LastPhoneNumber.Should().BeNone();
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllSmsesAndSome_ThenDrains()
    {
        var call = CallContext.CreateCustom("acallid", "acallerid", "atenantid");
        var messageId1 = CreateMessageId();
        var messageId2 = CreateMessageId();
        var messageId3 = CreateMessageId();
        await _smsMessageQueue.PushAsync(call, new SmsMessage
        {
            MessageId = messageId1,
            CallId = "acallid",
            CallerId = "acallerid",
            Message = new QueuedSmsMessage
            {
                Body = "anhtmlbody",
                ToPhoneNumber = "+6498876981",
                Tags = ["atag"]
            }
        }, CancellationToken.None);
        await _smsMessageQueue.PushAsync(call, new SmsMessage
        {
            MessageId = messageId2,
            CallId = "acallid",
            CallerId = "acallerid",
            Message = new QueuedSmsMessage
            {
                Body = "anhtmlbody",
                ToPhoneNumber = "+6498876982",
                Tags = ["atag"]
            }
        }, CancellationToken.None);
        await _smsMessageQueue.PushAsync(call, new SmsMessage
        {
            MessageId = messageId3,
            CallId = "acallid",
            CallerId = "acallerid",
            Message = new QueuedSmsMessage
            {
                Body = "anhtmlbody",
                ToPhoneNumber = "+6498876983",
                Tags = ["atag"]
            }
        }, CancellationToken.None);

        var request = new DrainAllSmsesRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _smsDeliveryService.AllPhoneNumbers.Count.Should().Be(3);
        _smsDeliveryService.AllPhoneNumbers.Should().ContainInOrder("+6498876981", "+6498876982", "+6498876983");
    }
#endif

    private static string CreateMessageId()
    {
        return new MessageQueueMessageIdFactory().Create("sms");
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<ISmsDeliveryService, StubSmsDeliveryService>();
    }
}