using System.Net;
using AncillaryInfrastructure.IntegrationTests.Stubs;
using ApiHost1;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Common.Extensions;
using Domain.Common;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class MailgunApiSpec : WebApiSpec<Program>
{
    private readonly StubEmailDeliveryService _emailDeliveryService;

    public MailgunApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _emailDeliveryService = setup.GetRequiredService<IEmailDeliveryService>().As<StubEmailDeliveryService>();
        _emailDeliveryService.Reset();
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndNotDeliveredEvent_ThenReturnsOk()
    {
        var result = await Api.PostAsync(new MailgunNotifyWebhookEventRequest
        {
            Signature = new MailgunSignature
            {
                Timestamp = "1",
                Token = "atoken",
                Signature = "bf106940253fa7477ba4b55a027126b70037ce9b00e67aa3bf4f5bab2775d3e1"
            },
            EventData = new MailgunEventData
            {
                Event = "anunknownevent"
            }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndDeliveredEvent_ThenReturnsOk()
    {
        var receiptId = await DeliveryEmailAsync();

        var deliveredAt = DateTime.UtcNow.ToUnixSeconds();
        var result = await Api.PostAsync(new MailgunNotifyWebhookEventRequest
        {
            Signature = new MailgunSignature
            {
                Timestamp = "1",
                Token = "atoken",
                Signature = "bf106940253fa7477ba4b55a027126b70037ce9b00e67aa3bf4f5bab2775d3e1"
            },
            EventData = new MailgunEventData
            {
                Event = MailgunEventType.Delivered.ToString(),
                Timestamp = deliveredAt,
                Message = new MailgunMessage
                {
                    Headers = new MailgunMessageHeaders
                    {
                        MessageId = receiptId
                    }
                }
            }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails![0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails![0].IsDelivered.Should().BeTrue();
        deliveries.Content.Value.Emails![0].DeliveredAt.Should().Be(deliveredAt.FromUnixTimestamp());
        deliveries.Content.Value.Emails![0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Emails![0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Emails![0].FailedDeliveryReason.Should().BeNull();
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndTemporaryFailedEvent_ThenReturnsOk()
    {
        var receiptId = await DeliveryEmailAsync();

        var failedAt = DateTime.UtcNow.ToUnixSeconds();
        var result = await Api.PostAsync(new MailgunNotifyWebhookEventRequest
        {
            Signature = new MailgunSignature
            {
                Timestamp = "1",
                Token = "atoken",
                Signature = "bf106940253fa7477ba4b55a027126b70037ce9b00e67aa3bf4f5bab2775d3e1"
            },
            EventData = new MailgunEventData
            {
                Event = MailgunEventType.Failed.ToString(),
                Severity = MailgunConstants.Values.TemporarySeverity,
                DeliveryStatus = new MailgunDeliveryStatus
                {
                    Description = "areason"
                },
                Timestamp = failedAt,
                Message = new MailgunMessage
                {
                    Headers = new MailgunMessageHeaders
                    {
                        MessageId = receiptId
                    }
                }
            }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails![0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails![0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Emails![0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Emails![0].IsDeliveryFailed.Should().BeFalse();
        deliveries.Content.Value.Emails![0].FailedDeliveryAt.Should().BeNull();
        deliveries.Content.Value.Emails![0].FailedDeliveryReason.Should().BeNull();
    }

    [Fact]
    public async Task WhenNotifyMailgunEventAndPermanentFailedEvent_ThenReturnsOk()
    {
        var receiptId = await DeliveryEmailAsync();

        var failedAt = DateTime.UtcNow.ToUnixSeconds();
        var result = await Api.PostAsync(new MailgunNotifyWebhookEventRequest
        {
            Signature = new MailgunSignature
            {
                Timestamp = "1",
                Token = "atoken",
                Signature = "bf106940253fa7477ba4b55a027126b70037ce9b00e67aa3bf4f5bab2775d3e1"
            },
            EventData = new MailgunEventData
            {
                Event = MailgunEventType.Failed.ToString(),
                Severity = MailgunConstants.Values.PermanentSeverity,
                DeliveryStatus = new MailgunDeliveryStatus
                {
                    Description = "areason"
                },
                Timestamp = failedAt,
                Message = new MailgunMessage
                {
                    Headers = new MailgunMessageHeaders
                    {
                        MessageId = receiptId
                    }
                }
            }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await LoginUserAsync(LoginUser.Operator);
        var deliveries = await Api.GetAsync(new SearchEmailDeliveriesRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        deliveries.Content.Value.Emails![0].IsSent.Should().BeTrue();
        deliveries.Content.Value.Emails![0].IsDelivered.Should().BeFalse();
        deliveries.Content.Value.Emails![0].DeliveredAt.Should().BeNull();
        deliveries.Content.Value.Emails![0].IsDeliveryFailed.Should().BeTrue();
        deliveries.Content.Value.Emails![0].FailedDeliveryAt.Should().Be(failedAt.FromUnixTimestamp());
        deliveries.Content.Value.Emails![0].FailedDeliveryReason.Should().Be("areason");
    }

    private async Task<string> DeliveryEmailAsync()
    {
        _emailDeliveryService.SendingSucceeds = true;
        var request = new SendEmailRequest
        {
            Message = new EmailMessage
            {
                MessageId = CreateMessageId(),
                CallId = "acallid",
                CallerId = "acallerid",
                Message = new QueuedEmailHtmlMessage
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
        var delivered = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));
        delivered.Content.Value.IsSent.Should().BeTrue();
        _emailDeliveryService.LastSubject.Should().Be("asubject");

        return _emailDeliveryService.LastReceiptId;
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IEmailDeliveryService, StubEmailDeliveryService>();
    }

    private static string CreateMessageId()
    {
        return new MessageQueueIdFactory().Create("email");
    }
}