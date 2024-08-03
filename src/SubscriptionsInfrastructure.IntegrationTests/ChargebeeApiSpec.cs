using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Application.Services.Shared;
using ChargeBee.Models;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionsInfrastructure.IntegrationTests.Stubs;
using Xunit;

namespace SubscriptionsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class ChargebeeApiSpec : WebApiSpec<Program>
{
    private readonly StubWebhookNotificationAuditService _stubAuditService;

    public ChargebeeApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _stubAuditService = setup.GetRequiredService<IWebhookNotificationAuditService>()
            .As<StubWebhookNotificationAuditService>();
        _stubAuditService.Reset();
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithUnknownEvent_ThenReturnsOk()
    {
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = "aununknowneventtype",
            Content = new ChargebeeEventContent()
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithCustomerChangedEventAndCustomerNotExist_ThenReturnsOk()
    {
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.CustomerChanged.ToString(),
            Content = new ChargebeeEventContent
            {
                Customer = new ChargebeeEventCustomer
                {
                    Id = "acustomerid",
                    PaymentMethod = new ChargebeePaymentMethod
                    {
                        Id = "apaymentmethodid",
                        Status = Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString().ToCamelCase(),
                        Type = Customer.CustomerPaymentMethod.TypeEnum.Card.ToString().ToCamelCase()
                    }
                }
            }
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _stubAuditService.LastProcessed.Should().BeNull();
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithCustomerChangedEvent_ThenReturnsOk()
    {
        var login = await LoginUserAsync();
        var subscription = (await Api.GetAsync(new GetSubscriptionRequest
            {
                Id = login.DefaultOrganizationId
            }, req => req.SetJWTBearerToken(login.AccessToken)))
            .Content.Value.Subscription;

        var customerId = subscription.BuyerReference;
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.CustomerChanged.ToString(),
            Content = new ChargebeeEventContent
            {
                Customer = new ChargebeeEventCustomer
                {
                    Id = customerId,
                    PaymentMethod = new ChargebeePaymentMethod
                    {
                        Id = "apaymentmethodid",
                        Status = Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString().ToCamelCase(),
                        Type = Customer.CustomerPaymentMethod.TypeEnum.Card.ToString().ToCamelCase()
                    }
                }
            }
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _stubAuditService.LastProcessed!.EventType.Should().Be(ChargebeeEventType.CustomerChanged.ToString());
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithCustomerDeletedEvent_ThenReturnsOk()
    {
        var login = await LoginUserAsync();
        var subscription = (await Api.GetAsync(new GetSubscriptionRequest
            {
                Id = login.DefaultOrganizationId
            }, req => req.SetJWTBearerToken(login.AccessToken)))
            .Content.Value.Subscription;

        var customerId = subscription.BuyerReference;
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.CustomerDeleted.ToString(),
            Content = new ChargebeeEventContent
            {
                Customer = new ChargebeeEventCustomer
                {
                    Id = customerId
                }
            }
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _stubAuditService.LastProcessed!.EventType.Should().Be(ChargebeeEventType.CustomerDeleted.ToString());
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionChangedEvent_ThenReturnsOk()
    {
        var login = await LoginUserAsync();
        var subscription = (await Api.GetAsync(new GetSubscriptionRequest
            {
                Id = login.DefaultOrganizationId
            }, req => req.SetJWTBearerToken(login.AccessToken)))
            .Content.Value.Subscription;

        var subscriptionId = subscription.SubscriptionReference;
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.SubscriptionChanged.ToString(),
            Content = new ChargebeeEventContent
            {
                Subscription = new ChargebeeEventSubscription
                {
                    Id = subscriptionId,
                    CustomerId = "acustomerid",
                    Status = "active"
                }
            }
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _stubAuditService.LastProcessed!.EventType.Should().Be(ChargebeeEventType.SubscriptionChanged.ToString());
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionCancelledEvent_ThenReturnsOk()
    {
        var login = await LoginUserAsync();
        var subscription = (await Api.GetAsync(new GetSubscriptionRequest
            {
                Id = login.DefaultOrganizationId
            }, req => req.SetJWTBearerToken(login.AccessToken)))
            .Content.Value.Subscription;

        var subscriptionId = subscription.SubscriptionReference;
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.SubscriptionCancelled.ToString(),
            Content = new ChargebeeEventContent
            {
                Subscription = new ChargebeeEventSubscription
                {
                    Id = subscriptionId,
                    CustomerId = "acustomerid",
                    Status = "active"
                }
            }
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _stubAuditService.LastProcessed!.EventType.Should().Be(ChargebeeEventType.SubscriptionCancelled.ToString());
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionDeletedEvent_ThenReturnsOk()
    {
        var login = await LoginUserAsync();
        var subscription = (await Api.GetAsync(new GetSubscriptionRequest
            {
                Id = login.DefaultOrganizationId
            }, req => req.SetJWTBearerToken(login.AccessToken)))
            .Content.Value.Subscription;

        var subscriptionId = subscription.SubscriptionReference;
        var result = await Api.PostAsync(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.SubscriptionDeleted.ToString(),
            Content = new ChargebeeEventContent
            {
                Subscription = new ChargebeeEventSubscription
                {
                    Id = subscriptionId,
                    CustomerId = "acustomerid",
                    Status = "active"
                }
            }
        }, req => req.SetBasicAuth("ausername"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _stubAuditService.LastProcessed!.EventType.Should().Be(ChargebeeEventType.SubscriptionDeleted.ToString());
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IWebhookNotificationAuditService, StubWebhookNotificationAuditService>();
        services.AddPerHttpRequest<IBillingProvider, StubChargebeeBillingProvider>();
    }
}