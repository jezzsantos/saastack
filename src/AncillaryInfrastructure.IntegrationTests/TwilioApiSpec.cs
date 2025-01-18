using System.Net;
using AncillaryApplication;
using AncillaryInfrastructure.IntegrationTests.Stubs;
using ApiHost1;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Common.Extensions;
using Domain.Common;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class TwilioApiSpec : WebApiSpec<Program>
{
    private readonly StubSmsDeliveryService _smsDeliveryService;

    public TwilioApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _smsDeliveryService = setup.GetRequiredService<ISmsDeliveryService>().As<StubSmsDeliveryService>();
        _smsDeliveryService.Reset();
    }

    [Fact]
    public async Task WhenNotifyTwilioEventAndNotDeliveredEvent_ThenReturnsOk()
    {
        var result = await Api.PostAsync(new TwilioNotifyWebhookEventRequest
        {
            MessageStatus = TwilioMessageStatus.Queued,
            MessageSid = "areceiptid",
            RawDlrDoneDate = null
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenNotifyTwilioEventAndDeliveredEvent_ThenReturnsOk()
    {
        var receiptId = await DeliverySmsAsync();

        var deliveredAt = DateTime.UtcNow.ToNearestMinute();
        var rawDlrDoneDate = deliveredAt.ToTwilioDateLong();
        var result = await Api.PostAsync(new TwilioNotifyWebhookEventRequest
        {
            MessageStatus = TwilioMessageStatus.Delivered,
            MessageSid = receiptId,
            RawDlrDoneDate = rawDlrDoneDate
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchAllSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeTrue();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().Be(deliveredAt);
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].FailedDeliveryReason.Should().BeNull();
    }

    [Fact]
    public async Task WhenNotifyTwilioEventAndFailedEvent_ThenReturnsOk()
    {
        var receiptId = await DeliverySmsAsync();

        var result = await Api.PostAsync(new TwilioNotifyWebhookEventRequest
        {
            MessageStatus = TwilioMessageStatus.Failed,
            MessageSid = receiptId,
            RawDlrDoneDate = null,
            ErrorCode = "anerror"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchAllSmsDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Smses.Count.Should().Be(1);
        deliveries.Content.Value.Smses[0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Smses[0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Smses[0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Smses[0].IsDeliveryFailed.Should().BeTrue();
        deliveries.Content.Value.Smses[0].FailedDeliveryAt.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        deliveries.Content.Value.Smses[0].FailedDeliveryReason.Should().Be("anerror");
    }

    private async Task<string> DeliverySmsAsync()
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
                    ToPhoneNumber = "+6498876986",
                    Body = "abody"
                }
            }.ToJson()!
        };
        var delivered = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));
        delivered.Content.Value.IsSent.Should().BeTrue();

        return _smsDeliveryService.LastReceiptId;
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<ISmsDeliveryService, StubSmsDeliveryService>();
    }

    private static string CreateMessageId()
    {
        return new MessageQueueMessageIdFactory().Create("sms");
    }
}