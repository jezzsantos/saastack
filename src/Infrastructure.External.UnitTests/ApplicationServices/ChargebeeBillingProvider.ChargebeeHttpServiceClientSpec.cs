using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using ChargeBee.Models;
using ChargeBee.Models.Enums;
using Common;
using Common.Extensions;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Infrastructure.External.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;
using PersonName = Application.Resources.Shared.PersonName;
using Subscription = ChargeBee.Models.Subscription;
using Invoice = ChargeBee.Models.Invoice;

namespace Infrastructure.External.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ChargebeeHttpServiceClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly ChargebeeHttpServiceClient _client;
    private readonly Mock<ChargebeeHttpServiceClient.IPricingPlansCache> _pricingPlanCache;
    private readonly Mock<IChargebeeClient> _serviceClient;

    public ChargebeeHttpServiceClientSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _serviceClient = new Mock<IChargebeeClient>();
        _pricingPlanCache = new Mock<ChargebeeHttpServiceClient.IPricingPlansCache>();
        _pricingPlanCache.Setup(ppc => ppc.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PricingPlans>.None);

        _client = new ChargebeeHttpServiceClient(recorder.Object, _serviceClient.Object, _pricingPlanCache.Object,
            "aninitialplanid", "afamilyid");
    }

    [Fact]
    public async Task WhenSubscribeAsyncAndImmediatelyWithDate_ThenReturnsError()
    {
        var buyer = CreateBuyer("abuyerid");
        var options = new SubscribeOptions
        {
            StartWhen = StartSubscriptionSchedule.Immediately,
            FutureTime = DateTime.UtcNow
        };
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<Customer>.None);
        _serviceClient.Setup(sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCustomer("acustomerid", false));
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(CreateCustomer("acustomerid", false), "asubscriptionid"));

        var result = await _client.SubscribeAsync(_caller.Object, buyer, options, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_Subscribe_ScheduleInvalid);
    }

    [Fact]
    public async Task WhenSubscribeAsyncAndScheduledInPast_ThenReturnsError()
    {
        var buyer = CreateBuyer("abuyerId");
        var options = new SubscribeOptions
        {
            StartWhen = StartSubscriptionSchedule.Scheduled,
            FutureTime = DateTime.UtcNow.SubtractHours(1)
        };
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<Customer>.None);
        _serviceClient.Setup(sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCustomer("acustomerid", false));
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(CreateCustomer("acustomerid", false), "asubscriptionid"));

        var result = await _client.SubscribeAsync(_caller.Object, buyer, options, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_Subscribe_ScheduleInvalid);
    }

    [Fact]
    public async Task
        WhenSubscribeAsyncWithImmediatelyAndCustomerExists_ThenCreatesSubscriptionForCustomerAndUpdatesBillingDetails()
    {
        var buyer = CreateBuyer("abuyerId");
        var options = new SubscribeOptions
        {
            StartWhen = StartSubscriptionSchedule.Immediately,
            FutureTime = null
        };
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", true);
        var subscription = CreateSubscription(customer, "asubscriptionid", "aplanid", datum, datum, datum);
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer.ToOptional());
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _client.SubscribeAsync(_caller.Object, buyer, options, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodStatus].Should()
            .Be(Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodType].Should()
            .Be(Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.CanceledAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindCustomerByIdAsync(_caller.Object, "anentityid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(sc => sc.UpdateCustomerForBuyerAsync(_caller.Object, "anentityid",
            buyer, It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(_caller.Object, "anentityid",
            buyer, It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateSubscriptionForCustomerAsync(_caller.Object, "acustomerid",
            It.Is<Subscriber>(sub =>
                sub.EntityId == "anentityid"
                && sub.EntityType == "anentitytype"
            ), "aninitialplanid", Optional<long>.None, Optional<long>.None,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task
        WhenSubscribeAsyncAndScheduledAndCustomerExists_ThenCreatesSubscriptionForCustomerAndUpdatesBillingDetails()
    {
        var buyer = CreateBuyer("abuyerId");
        var starts = DateTime.UtcNow.AddMinutes(1).ToNearestSecond();
        var options = new SubscribeOptions
        {
            StartWhen = StartSubscriptionSchedule.Scheduled,
            FutureTime = starts
        };
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", true);
        var subscription = CreateSubscription(customer, "asubscriptionid", "aplanid", datum, datum, datum);
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer.ToOptional());
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _client.SubscribeAsync(_caller.Object, buyer, options, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodStatus].Should()
            .Be(Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodType].Should()
            .Be(Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.CanceledAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindCustomerByIdAsync(_caller.Object, "anentityid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(sc => sc.UpdateCustomerForBuyerAsync(_caller.Object, "anentityid",
            buyer, It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(_caller.Object, "anentityid",
            buyer, It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateSubscriptionForCustomerAsync(_caller.Object, "acustomerid",
            It.Is<Subscriber>(sub =>
                sub.EntityId == "anentityid"
                && sub.EntityType == "anentitytype"
            ), "aninitialplanid", starts.ToUnixSeconds(), Optional<long>.None,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSubscribeAsyncAndCustomerNotExists_ThenCreatesCustomerAndSubscription()
    {
        var buyer = CreateBuyer("abuyerId");
        var starts = DateTime.UtcNow.AddMinutes(1).ToNearestSecond();
        var options = new SubscribeOptions
        {
            StartWhen = StartSubscriptionSchedule.Scheduled,
            FutureTime = starts
        };
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", true);
        var subscription = CreateSubscription(customer, "asubscriptionid", "aplanid", datum, datum, datum);
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<Customer>.None);
        _serviceClient.Setup(sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _client.SubscribeAsync(_caller.Object, buyer, options, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodStatus].Should()
            .Be(Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodType].Should()
            .Be(Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.CanceledAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindCustomerByIdAsync(_caller.Object, "anentityid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateCustomerForBuyerAsync(_caller.Object, "anentityid",
            It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(sc => sc.CreateSubscriptionForCustomerAsync(_caller.Object, "acustomerid",
            It.Is<Subscriber>(sub =>
                sub.EntityId == "anentityid"
                && sub.EntityType == "anentitytype"
            ), "aninitialplanid", starts.ToUnixSeconds(), Optional<long>.None,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenListAllPricingPlansAsyncAndCached_ThenReturnsCachedPlans()
    {
        var plans = new PricingPlans
        {
            Monthly = []
        };
        _pricingPlanCache.Setup(ppc => ppc.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans.ToOptional());

        var result = await _client.ListAllPricingPlansAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().Be(plans);
        _pricingPlanCache.Verify(ppc => ppc.GetAsync(It.IsAny<CancellationToken>()));
        _pricingPlanCache.Verify(ppc => ppc.SetAsync(It.IsAny<PricingPlans>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _serviceClient.Verify(sc =>
            sc.ListActiveItemPricesAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient
            .Verify(sc => sc.ListSwitchFeaturesAsync(It.IsAny<ICallerContext>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _serviceClient.Verify(sc =>
            sc.ListPlanChargesAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(sc =>
            sc.ListPlanEntitlementsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenListAllPricingPlansAsync_ThenReturnsPlans()
    {
        _serviceClient.Setup(sc =>
                sc.ListActiveItemPricesAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ItemPrice>
            {
                CreatePlanItemPrice("aplanid", 3),
                CreateChargeItemPrice("achargeid", 5)
            });
        _serviceClient
            .Setup(sc => sc.ListSwitchFeaturesAsync(It.IsAny<ICallerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Feature>
            {
                CreateFeature("afeatureid")
            });
        _serviceClient.Setup(sc =>
                sc.ListPlanChargesAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttachedItem>
            {
                CreateSetupAttachedItem("achargeid", "aplanid")
            });
        _serviceClient.Setup(sc =>
                sc.ListPlanEntitlementsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement>
            {
                CreateEntitlement("afeatureid")
            });

        var result = await _client.ListAllPricingPlansAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Daily.Should().BeEmpty();
        result.Value.Weekly.Should().BeEmpty();
        result.Value.Monthly.Should().ContainSingle(plan =>
            plan.Id == "anitempriceid"
            && plan.Period.Frequency == 2
            && plan.Period.Unit == PeriodFrequencyUnit.Month
            && plan.Cost == 0.03M
            && plan.SetupCost == 0.05M
            && plan.Currency == "USD"
            && plan.Description == "adescription"
            && plan.DisplayName == "anexternalname"
            && plan.FeatureSection[0].Features[0].IsIncluded == true
            && plan.FeatureSection[0].Features[0].Description == "adescription"
            && plan.IsRecommended == false
            && plan.Notes == "someinvoicenotes"
            && plan.Trial!.HasTrial == true
            && plan.Trial.Frequency == 1
            && plan.Trial.Unit == PeriodFrequencyUnit.Month
        );
        result.Value.Annually.Should().BeEmpty();
        result.Value.Eternally.Should().BeEmpty();
        _pricingPlanCache.Verify(ppc => ppc.GetAsync(It.IsAny<CancellationToken>()));
        _pricingPlanCache.Verify(ppc => ppc.SetAsync(It.IsAny<PricingPlans>(), It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.ListActiveItemPricesAsync(_caller.Object, "afamilyid",
            It.IsAny<CancellationToken>()));
        _serviceClient.Setup(sc => sc.ListSwitchFeaturesAsync(_caller.Object, It.IsAny<CancellationToken>()));
        _serviceClient.Setup(sc => sc.ListPlanChargesAsync(_caller.Object, "aplanid", It.IsAny<CancellationToken>()));
        _serviceClient.Setup(sc =>
            sc.ListPlanEntitlementsAsync(_caller.Object, "aplanid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllInvoicesAsyncAndMissingCustomerId_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = await _client.SearchAllInvoicesAsync(_caller.Object, provider, DateTime.UtcNow, DateTime.UtcNow,
            new SearchOptions(), CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_InvalidCustomerId);
    }

    [Fact]
    public async Task WhenSearchAllInvoicesAsync_ThenReturnsInvoices()
    {
        var from = DateTime.UtcNow.ToNearestSecond();
        var to = DateTime.UtcNow.AddHours(1).ToNearestSecond();
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" }
        }).Value;
        _serviceClient.Setup(sc => sc.SearchAllCustomerInvoicesAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice>
            {
                CreateInvoice("acustomerid")
            });

        var result = await _client.SearchAllInvoicesAsync(_caller.Object, provider, from, to,
            new SearchOptions(), CancellationToken.None);

        result.Should().BeSuccess();
        var today = DateTime.Today.ToUniversalTime();
        var yesterday = today.SubtractDays(1);
        result.Value.Results.Should().ContainSingle(invoice =>
            invoice.Id == "aninvoiceid"
            && invoice.Amount == 0.09M
            && invoice.Currency == "USD"
            && invoice.IncludesTax == true
            && invoice.InvoicedOnUtc!.Value == today
            && invoice.LineItems.Count == 1
            && invoice.LineItems[0].Reference == "alineitemid"
            && invoice.LineItems[0].Description == "adescription"
            && invoice.LineItems[0].Amount == 0.09M
            && invoice.LineItems[0].Currency == "USD"
            && invoice.LineItems[0].IsTaxed
            && invoice.LineItems[0].TaxAmount == 0.08M
            && invoice.Notes.Count == 1
            && invoice.Notes[0].Description == "anotedescription"
            && invoice.Status == InvoiceStatus.Paid
            && invoice.TaxAmount == 0.07M
            && invoice.Payment!.Amount == 0.05M
            && invoice.Payment.Currency == "USD"
            && invoice.Payment.PaidOnUtc == today
            && invoice.Payment.Reference == "atransactionid"
            && invoice.PeriodStartUtc == yesterday
            && invoice.PeriodEndUtc == today
        );
        _serviceClient.Verify(sc => sc.SearchAllCustomerInvoicesAsync(_caller.Object, "acustomerid",
            from, to, It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanAsyncAndMissingCustomerId_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;

        var result = await _client.ChangeSubscriptionPlanAsync(_caller.Object, new ChangePlanOptions
        {
            PlanId = "aplanid",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            }
        }, provider, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_InvalidCustomerId);
        _serviceClient.Verify(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        _serviceClient.Verify(
            sc => sc.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanAsyncAndMissingSubscriptionId_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" }
        }).Value;

        var result = await _client.ChangeSubscriptionPlanAsync(_caller.Object, new ChangePlanOptions
        {
            PlanId = "aplanid",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            }
        }, provider, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_InvalidSubscriptionId);
        _serviceClient.Verify(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        _serviceClient.Verify(
            sc => sc.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanAsyncAndActivated_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Active.ToString() }
        }).Value;
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", false);
        var subscription = CreateSubscription(customer, "asubscriptionid");
        var changedSubscription = CreateSubscription(customer, "asubscriptionid", "aplanid2", datum, null, datum);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(changedSubscription);

        var result = await _client.ChangeSubscriptionPlanAsync(_caller.Object, new ChangePlanOptions
        {
            PlanId = "aplanid",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            }
        }, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid2");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.ChangeSubscriptionPlanAsync(_caller.Object, "asubscriptionid", "aplanid", Optional<long>.None,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanAsyncAndCanceling_ThenRemovesCancellationAndChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Active.ToString() }
        }).Value;
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", false);
        var subscription = CreateSubscription(customer, "asubscriptionid", status: Subscription.StatusEnum.NonRenewing);
        var removedSubscription =
            CreateSubscription(customer, "asubscriptionid", status: Subscription.StatusEnum.Active);
        var changedSubscription = CreateSubscription(customer, "asubscriptionid", "aplanid2", datum, null, datum);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.RemoveScheduledSubscriptionCancellationAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(removedSubscription);
        _serviceClient.Setup(sc => sc.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(changedSubscription);

        var result = await _client.ChangeSubscriptionPlanAsync(_caller.Object, new ChangePlanOptions
        {
            PlanId = "aplanid",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            }
        }, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid2");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Setup(sc =>
            sc.RemoveScheduledSubscriptionCancellationAsync(_caller.Object, "asubscriptionid",
                It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.ChangeSubscriptionPlanAsync(_caller.Object, "asubscriptionid", "aplanid", Optional<long>.None,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanAsyncAndCanceled_ThenReactivatesAndChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Active.ToString() }
        }).Value;
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", false);
        var subscription = CreateSubscription(customer, "asubscriptionid", status: Subscription.StatusEnum.Cancelled);
        var removedSubscription =
            CreateSubscription(customer, "asubscriptionid", status: Subscription.StatusEnum.Active);
        var changedSubscription = CreateSubscription(customer, "asubscriptionid", "aplanid2", datum, null, datum);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.ReactivateSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(removedSubscription);
        _serviceClient.Setup(sc => sc.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(changedSubscription);

        var result = await _client.ChangeSubscriptionPlanAsync(_caller.Object, new ChangePlanOptions
        {
            PlanId = "aplanid",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            }
        }, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid2");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Setup(sc => sc.ReactivateSubscriptionAsync(_caller.Object, "asubscriptionid",
            datum.ToUnixSeconds(), It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.ChangeSubscriptionPlanAsync(_caller.Object, "asubscriptionid", "aplanid", Optional<long>.None,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanAsyncAndUnsubscribed_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "true" }
        }).Value;
        var datum = DateTime.UtcNow.ToNearestSecond();
        var customer = CreateCustomer("acustomerid", false);
        var removedSubscription =
            CreateSubscription(customer, "asubscriptionid", status: Subscription.StatusEnum.Active);
        var changedSubscription = CreateSubscription(customer, "asubscriptionid", "aplanid2", datum, null, datum);
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(removedSubscription);
        _serviceClient.Setup(sc => sc.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(changedSubscription);

        var result = await _client.ChangeSubscriptionPlanAsync(_caller.Object, new ChangePlanOptions
        {
            PlanId = "aplanid",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            }
        }, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid2");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.TrialEnd].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(
            sc => sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(_caller.Object, "acustomerid",
            It.Is<Subscriber>(sub =>
                sub.EntityId == "anentityid"
                && sub.EntityType == "anentitytype"
            ), "aplanid", Optional<long>.None, Optional<long>.None, It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.ChangeSubscriptionPlanAsync(_caller.Object, "asubscriptionid", "aplanid", Optional<long>.None,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndImmediatelyWithDate_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;
        var options = new CancelSubscriptionOptions
        {
            CancelWhen = CancelSubscriptionSchedule.Immediately,
            FutureTime = DateTime.UtcNow
        };

        var result = await _client.CancelSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_Cancel_ScheduleInvalid);
        _serviceClient.Verify(
            sc => sc.CancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndScheduledInPast_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;
        var options = new CancelSubscriptionOptions
        {
            CancelWhen = CancelSubscriptionSchedule.Scheduled,
            FutureTime = DateTime.UtcNow.SubtractHours(1)
        };

        var result = await _client.CancelSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_Cancel_ScheduleInvalid);
        _serviceClient.Verify(
            sc => sc.CancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndImmediate_ThenCancelsEndOfTerm()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;
        var options = new CancelSubscriptionOptions
        {
            CancelWhen = CancelSubscriptionSchedule.Immediately,
            FutureTime = null
        };
        var datum = DateTime.UtcNow.ToNearestSecond();
        var subscription = CreateSubscription(CreateCustomer("acustomerid", false), "asubscriptionid", "aplanid", null,
            datum,
            datum);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.CancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _client.CancelSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.CanceledAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CancelSubscriptionAsync(_caller.Object, "asubscriptionid", false,
            Optional<long>.None, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndEndOfTerm_ThenCancelsEndOfTerm()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;
        var options = new CancelSubscriptionOptions
        {
            CancelWhen = CancelSubscriptionSchedule.EndOfTerm,
            FutureTime = null
        };
        var datum = DateTime.UtcNow.ToNearestSecond();
        var subscription = CreateSubscription(CreateCustomer("acustomerid", false), "asubscriptionid", "aplanid", null,
            datum,
            datum);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.CancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _client.CancelSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.CanceledAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CancelSubscriptionAsync(_caller.Object, "asubscriptionid", true,
            Optional<long>.None, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndScheduled_ThenCancelsEndOfTerm()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;
        var canceledAt = DateTime.UtcNow.AddHours(1).ToNearestSecond();
        var options = new CancelSubscriptionOptions
        {
            CancelWhen = CancelSubscriptionSchedule.Scheduled,
            FutureTime = canceledAt
        };
        var datum = DateTime.UtcNow.ToNearestSecond();
        var subscription = CreateSubscription(CreateCustomer("acustomerid", false), "asubscriptionid", "aplanid", null,
            datum,
            datum);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.CancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<Optional<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _client.CancelSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionId].Should().Be("asubscriptionid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodValue].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.BillingPeriodUnit].Should()
            .Be(Subscription.BillingPeriodUnitEnum.Month.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionStatus].Should()
            .Be(Subscription.StatusEnum.Active.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.SubscriptionDeleted].Should().Be(false.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.CurrencyCode].Should().Be("USD");
        result.Value[ChargebeeConstants.MetadataProperties.PlanId].Should().Be("aplanid");
        result.Value[ChargebeeConstants.MetadataProperties.BillingAmount].Should().Be("0");
        result.Value[ChargebeeConstants.MetadataProperties.NextBillingAt].Should().Be(datum.ToIso8601());
        result.Value[ChargebeeConstants.MetadataProperties.CanceledAt].Should().Be(datum.ToIso8601());
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CancelSubscriptionAsync(_caller.Object, "asubscriptionid", false,
            canceledAt.ToUnixSeconds(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncWithEmptyBuyerSubscriber_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;
        var options = new TransferSubscriptionOptions
        {
            TransfereeBuyer = new SubscriptionBuyer
            {
                Subscriber = new Subscriber
                {
                    EntityId = string.Empty,
                    EntityType = string.Empty
                },
                Address = new ProfileAddress
                {
                    CountryCode = "acountrycode"
                },
                EmailAddress = "anemailaddress",
                Id = "abuyerid",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                }
            }
        };

        var result = await _client.TransferSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ChargebeeHttpServiceClient_Transfer_BuyerInvalid);
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task
        WhenTransferSubscriptionAsyncAndUnsubscribedWithNoPlan_ThenCreatesNewSubscriptionOnInitialPlanAndUpdatesCustomer()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Active.ToString() }
        }).Value;
        var buyer = new SubscriptionBuyer
        {
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            },
            Address = new ProfileAddress
            {
                CountryCode = "acountrycode"
            },
            EmailAddress = "anemailaddress",
            Id = "abuyerid",
            Name = new PersonName
            {
                FirstName = "afirstname"
            }
        };
        var options = new TransferSubscriptionOptions
        {
            TransfereeBuyer = buyer
        };
        var customer = CreateCustomer("acustomerid", false);
        var subscription =
            CreateSubscription(customer, "asubscriptionid", "aplanid", null, null, null,
                Subscription.StatusEnum.UnKnown);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _client.TransferSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeSuccess();
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateSubscriptionForCustomerAsync(_caller.Object,
            "acustomerid", It.Is<Subscriber>(sub =>
                sub.EntityId == "anentityid"
                && sub.EntityType == "anentitytype"
            ), "aninitialplanid", Optional<long>.None, 0,
            It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), "anentityid", buyer,
                It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(), "anentityid", buyer,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task
        WhenTransferSubscriptionAsyncAndUnsubscribedWithPlan_ThenCreatesNewSubscriptionAndUpdatesCustomer()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
            { ChargebeeConstants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Active.ToString() }
        }).Value;
        var buyer = new SubscriptionBuyer
        {
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            },
            Address = new ProfileAddress
            {
                CountryCode = "acountrycode"
            },
            EmailAddress = "anemailaddress",
            Id = "abuyerid",
            Name = new PersonName
            {
                FirstName = "afirstname"
            }
        };
        var options = new TransferSubscriptionOptions
        {
            TransfereeBuyer = buyer,
            PlanId = "anotherplanid"
        };
        var customer = CreateCustomer("acustomerid", false);
        var subscription =
            CreateSubscription(customer, "asubscriptionid", "aplanid", null, null, null,
                Subscription.StatusEnum.UnKnown);
        _serviceClient.Setup(sc =>
                sc.FindSubscriptionByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _serviceClient.Setup(sc => sc.CreateSubscriptionForCustomerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<Subscriber>(), It.IsAny<string>(), It.IsAny<Optional<long>>(), It.IsAny<Optional<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _client.TransferSubscriptionAsync(_caller.Object, options, provider, CancellationToken.None);

        result.Should().BeSuccess();
        _serviceClient.Verify(sc =>
            sc.FindSubscriptionByIdAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateSubscriptionForCustomerAsync(_caller.Object,
            "acustomerid", It.Is<Subscriber>(sub =>
                sub.EntityId == "anentityid"
                && sub.EntityType == "anentitytype"
            ), "anotherplanid", Optional<long>.None, 0,
            It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), "anentityid", buyer,
                It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(), "anentityid", buyer,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRestoreBuyerAsyncAndCustomerExists_ThenUpdatesCustomer()
    {
        var buyer = CreateBuyer("abuyerid");
        var customer = CreateCustomer("acustomerid", true);
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer.ToOptional());
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _serviceClient.Setup(sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _client.RestoreBuyerAsync(_caller.Object, buyer, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodStatus].Should()
            .Be(Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString());
        result.Value[ChargebeeConstants.MetadataProperties.PaymentMethodType].Should()
            .Be(Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
        _serviceClient.Verify(sc =>
            sc.FindCustomerByIdAsync(_caller.Object, "anentityid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
            It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerAsync(_caller.Object, "anentityid",
                buyer, It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerBillingAddressAsync(_caller.Object, "anentityid",
                buyer, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRestoreBuyerAsyncAndCustomerNotExists_ThenCreatesCustomer()
    {
        var buyer = CreateBuyer("abuyerid");
        var customer = CreateCustomer("acustomerid", false);
        _serviceClient.Setup(sc =>
                sc.FindCustomerByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<Customer>.None);
        _serviceClient.Setup(sc => sc.CreateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _client.RestoreBuyerAsync(_caller.Object, buyer, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("acustomerid");
        _serviceClient.Verify(sc =>
            sc.FindCustomerByIdAsync(_caller.Object, "anentityid", It.IsAny<CancellationToken>()));
        _serviceClient.Verify(sc => sc.CreateCustomerForBuyerAsync(_caller.Object, "anentityid",
            It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()));
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceClient.Verify(
            sc => sc.UpdateCustomerForBuyerBillingAddressAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Invoice CreateInvoice(string customerId)
    {
        var today = DateTime.Today.ToUniversalTime();
        var yesterday = today.SubtractDays(1);
        var invoice = new
        {
            id = "aninvoiceid",
            customer_id = customerId,
            total = 9,
            currency_code = "USD",
            price_type = PriceTypeEnum.TaxInclusive.ToString(true),
            date = today.ToUnixSeconds(),
            paid_at = today.ToUnixSeconds(),
            line_items = new[]
            {
                new
                {
                    id = "alineitemid",
                    description = "adescription",
                    amount = 9,
                    currency_code = "USD",
                    is_taxed = true,
                    tax_amount = 8,
                    date_from = yesterday.ToUnixSeconds(),
                    date_to = today.ToUnixSeconds()
                }
            },
            notes = new[]
            {
                new
                {
                    note = "anotedescription"
                }
            },
            status = Invoice.StatusEnum.Paid.ToString(true),
            tax = 7,
            linked_payments = new[]
            {
                new
                {
                    txn_id = "atransactionid"
                }
            },
            amount_paid = 5
        };

        return new Invoice(invoice.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static Entitlement CreateEntitlement(string featureId)
    {
        var attachedItem = new
        {
            id = "anentitlementid",
            feature_id = featureId
        };

        return new Entitlement(attachedItem.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static AttachedItem CreateSetupAttachedItem(string itemId, string planId)
    {
        var attachedItem = new
        {
            id = "anattacheditemid",
            item_id = itemId,
            parent_item_id = planId,
            status = AttachedItem.StatusEnum.Active.ToString(true),
            charge_on_event = ChargeOnEventEnum.SubscriptionCreation.ToString(true)
        };

        return new AttachedItem(attachedItem.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static Feature CreateFeature(string featureId)
    {
        var feature = new
        {
            id = featureId,
            description = "adescription"
        };

        return new Feature(feature.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static ItemPrice CreatePlanItemPrice(string planId, decimal price)
    {
        var itemPrice = new
        {
            id = "anitempriceid",
            item_id = planId,
            item_type = ItemTypeEnum.Plan.ToString(true),
            currency_code = "USD",
            description = "adescription",
            external_name = "anexternalname",
            invoice_notes = "someinvoicenotes",
            trial_period = 1,
            trial_period_unit = ItemPrice.TrialPeriodUnitEnum.Month.ToString(true),
            period = 2,
            period_unit = ItemPrice.PeriodUnitEnum.Month.ToString(true),
            price
        };

        return new ItemPrice(itemPrice.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static ItemPrice CreateChargeItemPrice(string chargeId, decimal price, string currencyCode = "USD")
    {
        var itemPrice = new
        {
            id = "anitempriceid",
            item_id = chargeId,
            item_type = ItemTypeEnum.Charge.ToString(true),
            currency_code = currencyCode,
            description = "asetupcharge",
            external_name = "anexternalname",
            price
        };

        return new ItemPrice(itemPrice.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static Subscription CreateSubscription(Customer customer, string subscriptionId, string planId = "aplanid",
        DateTime? trialEndsAt = null,
        DateTime? canceledAt = null, DateTime? nextBilledAt = null,
        Subscription.StatusEnum status = Subscription.StatusEnum.Active)
    {
        var subscription = new
        {
            id = subscriptionId,
            customer_id = customer.Id,
            status = status.ToString(true),
            deleted = false,
            cancelled_at = canceledAt.HasValue
                ? canceledAt.ToUnixSeconds()
                : (long?)null,
            billing_period = 0,
            billing_period_unit = Subscription.BillingPeriodUnitEnum.Month.ToString(true),
            subscription_items = new[]
            {
                new
                {
                    item_price_id = planId,
                    amount = 0
                }
            },
            trial_end = trialEndsAt.HasValue
                ? trialEndsAt.ToUnixSeconds()
                : (long?)null,
            currency_code = "USD",
            next_billing_at = nextBilledAt.HasValue
                ? nextBilledAt.ToUnixSeconds()
                : (long?)null
        };

        return new Subscription(subscription.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static Customer CreateCustomer(string customerId, bool hasPaymentMethod)
    {
        var customer = new
        {
            id = customerId,
            payment_method = hasPaymentMethod
                ? new
                {
                    status = Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString(true),
                    type = Customer.CustomerPaymentMethod.TypeEnum.Card.ToString(true)
                }
                : null
        };
        return new Customer(customer.ToJson(false, StringExtensions.JsonCasing.Camel));
    }

    private static SubscriptionBuyer CreateBuyer(string buyerId)
    {
        return new SubscriptionBuyer
        {
            Id = buyerId,
            Name = new PersonName
            {
                FirstName = "afirstname",
                LastName = "alastname"
            },
            EmailAddress = "anemailaddress",
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            },
            PhoneNumber = "aphonenumber",
            Address = new ProfileAddress
            {
                Line1 = "aline1",
                Line2 = "aline2",
                City = "acity",
                State = "astate",
                Zip = "azip",
                CountryCode = "acountrycode"
            }
        };
    }
}